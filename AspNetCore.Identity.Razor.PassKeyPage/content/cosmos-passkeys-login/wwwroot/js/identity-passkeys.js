(function () {
    function base64UrlToUint8Array(base64Url) {
        if (!base64Url) return new Uint8Array(0);
        var padding = "=".repeat((4 - (base64Url.length % 4)) % 4);
        var base64 = (base64Url + padding).replace(/-/g, "+").replace(/_/g, "/");
        var raw = atob(base64);
        var bytes = new Uint8Array(raw.length);
        for (var i = 0; i < raw.length; i++) bytes[i] = raw.charCodeAt(i);
        return bytes;
    }

    function uint8ArrayToBase64Url(buffer) {
        var bytes = buffer instanceof ArrayBuffer ? new Uint8Array(buffer) : buffer;
        var binary = "";
        for (var i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i]);
        return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/g, "");
    }

    function normalizeRequestOptions(options) {
        var normalized = options && options.publicKey ? options : { publicKey: options };
        if (!normalized || !normalized.publicKey) throw new Error("Invalid passkey options response.");

        normalized.publicKey.challenge = base64UrlToUint8Array(normalized.publicKey.challenge);

        if (Array.isArray(normalized.publicKey.allowCredentials)) {
            normalized.publicKey.allowCredentials = normalized.publicKey.allowCredentials.map(function (c) {
                return {
                    type: c.type,
                    id: base64UrlToUint8Array(c.id),
                    transports: c.transports
                };
            });
        }

        return normalized;
    }

    function buildAssertionPayload(assertion) {
        return {
            id: assertion.id,
            rawId: uint8ArrayToBase64Url(assertion.rawId),
            type: assertion.type,
            response: {
                clientDataJSON: uint8ArrayToBase64Url(assertion.response.clientDataJSON),
                authenticatorData: uint8ArrayToBase64Url(assertion.response.authenticatorData),
                signature: uint8ArrayToBase64Url(assertion.response.signature),
                userHandle: assertion.response.userHandle ? uint8ArrayToBase64Url(assertion.response.userHandle) : null
            },
            clientExtensionResults: assertion.getClientExtensionResults ? assertion.getClientExtensionResults() : {}
        };
    }

    function antiForgeryToken(form) {
        var token = form.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : null;
    }

    function endpointFromAttributes(button, form, names) {
        for (var i = 0; i < names.length; i++) {
            var name = names[i];
            var b = button.getAttribute(name);
            if (b) return b;
            var f = form.getAttribute(name);
            if (f) return f;
        }
        return null;
    }

    function handlerUrl(form, handlerName) {
        var action = form.action || window.location.href;
        var url = new URL(action, window.location.origin);
        url.searchParams.set("handler", handlerName);
        return url.toString();
    }

    async function postJson(url, body, token) {
        var headers = { "Content-Type": "application/json" };
        if (token) headers["RequestVerificationToken"] = token;

        var response = await fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: headers,
            body: JSON.stringify(body || {})
        });

        if (!response.ok) throw new Error("Request failed: " + response.status);
        return await response.json().catch(function () { return {}; });
    }

    async function getOptions(button, form, username, token) {
        var explicit = endpointFromAttributes(button, form, [
            "data-passkey-options-url",
            "data-passkey-auth-options-url",
            "data-passkey-challenge-url"
        ]);

        var candidates = [
            explicit,
            handlerUrl(form, "PasskeyAssertionOptions"),
            handlerUrl(form, "PasskeyAuthenticationOptions"),
            handlerUrl(form, "PasskeyOptions")
        ].filter(Boolean);

        var lastError = null;
        for (var i = 0; i < candidates.length; i++) {
            try {
                return await postJson(candidates[i], { userName: username || "" }, token);
            } catch (e) {
                lastError = e;
            }
        }

        throw lastError || new Error("No passkey options endpoint available.");
    }

    async function verify(button, form, payload, token) {
        var explicit = endpointFromAttributes(button, form, [
            "data-passkey-verify-url",
            "data-passkey-authenticate-url",
            "data-passkey-login-url"
        ]);

        var candidates = [
            explicit,
            handlerUrl(form, "PasskeyLogin"),
            handlerUrl(form, "PasskeyAuthenticate"),
            handlerUrl(form, "PasskeyAssertion")
        ].filter(Boolean);

        var lastError = null;
        for (var i = 0; i < candidates.length; i++) {
            try {
                return await postJson(candidates[i], payload, token);
            } catch (e) {
                lastError = e;
            }
        }

        throw lastError || new Error("No passkey verify endpoint available.");
    }

    async function loginWithPasskey(button, form) {
        var token = antiForgeryToken(form);
        var userInput = form.querySelector('#Input_Email, input[name="Input.Email"], input[name="Email"], input[name="username"]');
        var username = userInput ? userInput.value : "";

        var options = await getOptions(button, form, username, token);
        var publicKey = normalizeRequestOptions(options);
        var assertion = await navigator.credentials.get(publicKey);
        if (!assertion) throw new Error("No credential returned.");

        var payload = buildAssertionPayload(assertion);
        var result = await verify(button, form, payload, token);

        if (result && result.redirectUrl) {
            window.location.assign(result.redirectUrl);
            return;
        }

        if (result && result.success === false && result.message) {
            throw new Error(result.message);
        }

        window.location.reload();
    }

    function init() {
        var button = document.getElementById("passkey-login-submit");
        if (!button) return;

        button.addEventListener("click", async function (event) {
            event.preventDefault();

            if (!window.PublicKeyCredential) {
                console.warn("Passkeys are not supported in this browser.");
                return;
            }

            var form = button.closest("form");
            if (!form) {
                console.warn("Passkey login form was not found.");
                return;
            }

            try {
                await loginWithPasskey(button, form);
            } catch (error) {
                console.warn(error && error.message ? error.message : error);
            }
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
