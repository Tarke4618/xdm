"use strict";
import Logger from './logger.js';

const APP_BASE_URL = "http://127.0.0.1:8597";
const NATIVE_HOST_NAME = "xdm_chrome.native_host";

export default class Connector {
    constructor(onMessage, onDisconnect) {
        this.logger = new Logger();
        this.onMessage = onMessage;
        this.onDisconnect = onDisconnect;
        this.connected = false;
        
        // Mode: 'native' | 'http' | 'none'
        this.mode = 'none';
        this.port = null;
        this.httpTimer = null;
    }

    connect() {
        this.logger.log("Attempting to connect via Native Messaging...");
        this.connectNative();
    }

    connectNative() {
        try {
            this.port = chrome.runtime.connectNative(NATIVE_HOST_NAME);
            this.port.onMessage.addListener(this.onNativeMessage.bind(this));
            this.port.onDisconnect.addListener(this.onNativeDisconnect.bind(this));
            this.mode = 'native';
            this.connected = true;
            this.logger.log("Native messaging port opened.");
        } catch (err) {
            this.logger.log("Native messaging connection failed, switching to HTTP fallback.");
            this.startHttpFallback();
        }
    }

    onNativeMessage(msg) {
        this.connected = true;
        this.onMessage(msg);
    }

    onNativeDisconnect() {
        const err = chrome.runtime.lastError;
        this.logger.log("Native messaging disconnected: " + (err ? err.message : "No error message."));
        this.port = null;

        // If it disconnected immediately, it's likely not registered. Switch to HTTP loopback.
        if (this.mode === 'native') {
            this.logger.log("Switching to HTTP fallback loop...");
            this.startHttpFallback();
        } else {
            this.disconnect();
        }
    }

    startHttpFallback() {
        this.mode = 'http';
        this.cleanupNative();

        if (this.httpTimer) {
            clearInterval(this.httpTimer);
        }

        // Poll HTTP loopback
        this.httpTimer = setInterval(() => {
            this.pollHttpSync();
        }, 5000);

        this.pollHttpSync();
    }

    pollHttpSync() {
        fetch(APP_BASE_URL + "/sync")
            .then(res => {
                this.connected = true;
                res.json()
                    .then(json => this.onMessage(json))
                    .catch(err => this.disconnect());
            })
            .catch(err => {
                this.logger.log("HTTP sync failed: " + err.message);
                this.disconnect();
            });
    }

    cleanupNative() {
        if (this.port) {
            try { this.port.disconnect(); } catch (e) {}
            this.port = null;
        }
    }

    disconnect() {
        this.connected = false;
        this.onDisconnect();
    }

    isConnected() {
        return this.connected;
    }

    postMessage(url, data) {
        const type = url.replace("/", ""); // Map e.g. "/download" to "download"
        
        if (this.mode === 'native' && this.port) {
            try {
                this.port.postMessage({
                    type: type,
                    data: data
                });
                return;
            } catch (err) {
                this.logger.log("Failed to post message via native port, falling back: " + err.message);
                this.startHttpFallback();
            }
        }

        // HTTP fallback
        fetch(APP_BASE_URL + url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(data)
        })
        .then(res => {
            this.connected = true;
            res.json()
                .then(json => this.onMessage(json))
                .catch(err => this.disconnect());
        })
        .catch(err => this.disconnect());
    }

    launchApp() {
        if (this.mode === 'native' && this.port) {
            // Under native messaging, launching the port automatically spawns XDM if Mutex is missing.
            this.logger.log("Requesting application launch...");
            this.port.postMessage({ type: "launch" });
        } else {
            // HTTP mode: cannot directly launch, rely on native host if registered.
            chrome.runtime.sendNativeMessage(NATIVE_HOST_NAME, { type: "launch" }, (response) => {
                if (chrome.runtime.lastError) {
                    this.logger.log("Unable to launch XDM via native bridge: " + chrome.runtime.lastError.message);
                }
            });
        }
    }
}