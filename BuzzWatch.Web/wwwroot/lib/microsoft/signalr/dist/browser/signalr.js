// This is a placeholder for the SignalR client library
// In a real application, you would include the actual library
// downloaded from https://cdn.jsdelivr.net/npm/@microsoft/signalr/dist/browser/signalr.min.js

// Minimal mock implementation for demo purposes
(function (global) {
    const signalR = {
        HubConnectionBuilder: class {
            constructor() {
                this.config = {};
            }
            
            withUrl(url, options) {
                this.config.url = url;
                this.config.options = options;
                return this;
            }
            
            withAutomaticReconnect() {
                this.config.reconnect = true;
                return this;
            }
            
            build() {
                return new HubConnection(this.config);
            }
        }
    };
    
    class HubConnection {
        constructor(config) {
            this.config = config;
            this.callbacks = {};
            console.log('SignalR hub connection created with', config);
        }
        
        on(methodName, callback) {
            this.callbacks[methodName] = callback;
            console.log(`SignalR: Registered handler for '${methodName}'`);
        }
        
        start() {
            console.log('SignalR: Connection started');
            return Promise.resolve();
        }
        
        onclose(callback) {
            this.onCloseCallback = callback;
        }
        
        // Helper for demo/testing
        _mockReceive(methodName, data) {
            if (this.callbacks[methodName]) {
                this.callbacks[methodName](data);
            }
        }
    }
    
    global.signalR = signalR;
})(window); 