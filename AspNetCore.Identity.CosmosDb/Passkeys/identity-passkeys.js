(function () {
    function parseOptionsJson(response) {
        if (!response.ok) {
            throw new Error("The server responded with status " + response.status + ".");
        }
        return response.json();
    }

    function getAntiforgeryToken() {
        const input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : "";
    }

    function fetchWithDefaults(url, options) {
        const token = getAntiforgeryToken();
        const providedOptions = options || {};
        const headers = Object.assign({}, providedOptions.headers || {});

        if (token) {
            headers["RequestVerificationToken"] = token;
        }

        const requestOptions = Object.assign({}, providedOptions);
        requestOptions.credentials = "include";
        requestOptions.headers = headers;

        return fetch(url, requestOptions);
    }

    function convertToBase64Url(value) {
        if (!value) {
            return undefined;
        }

        let bytes = value;
        if (Array.isArray(bytes)) {
            bytes = Uint8Array.from(bytes);
        }
        if (bytes instanceof ArrayBuffer) {
            bytes = new Uint8Array(bytes);
        }
        if (!(bytes instanceof Uint8Array)) {
            return undefined;
        }

        let binary = "";
        for (let i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }

        return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/g, "");
    }

    function serializeCredential(credential) {
        try {
            return JSON.stringify(credential);
        } catch {
            return JSON.stringify({
                authenticatorAttachment: credential.authenticatorAttachment,
                clientExtensionResults: credential.getClientExtensionResults(),
                id: credential.id,
                rawId: convertToBase64Url(credential.rawId),
                response: {
                    attestationObject: convertToBase64Url(credential.response.attestationObject),
                    authenticatorData: convertToBase64Url(credential.response.authenticatorData ?? credential.response.getAuthenticatorData?.()),
                    clientDataJSON: convertToBase64Url(credential.response.clientDataJSON),
                    publicKey: convertToBase64Url(credential.response.getPublicKey?.()),
                    publicKeyAlgorithm: credential.response.getPublicKeyAlgorithm?.(),
                    transports: credential.response.getTransports?.(),
                    signature: convertToBase64Url(credential.response.signature),
                    userHandle: convertToBase64Url(credential.response.userHandle)
                },
                type: credential.type
            });
        }
    }

    function clearStatus(statusElement) {
        if (!statusElement) {
            return;
        }

        statusElement.classList.add("d-none");
        statusElement.textContent = "";
        statusElement.classList.remove("alert-success", "alert-danger", "alert-info", "alert-warning");
    }

    function showStatus(statusElement, message, type) {
        if (!statusElement) {
            return;
        }

        statusElement.classList.remove("d-none", "alert-success", "alert-danger", "alert-info", "alert-warning");
        statusElement.classList.add("alert-" + type);
        statusElement.textContent = message;
    }

    function encodeHtml(value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    async function addPasskeyAsync(apiBase, statusElement) {
        if (!window.PublicKeyCredential || !navigator.credentials) {
            throw new Error("Passkeys are not supported in this browser.");
        }

        const optionsResponse = await fetchWithDefaults(apiBase + "/creation-options", { method: "POST" });
        const optionsJson = await parseOptionsJson(optionsResponse);
        const publicKeyOptions = PublicKeyCredential.parseCreationOptionsFromJSON(optionsJson);
        const credential = await navigator.credentials.create({ publicKey: publicKeyOptions });

        if (!credential) {
            throw new Error("No credential was returned by the browser.");
        }

        const providedName = window.prompt("Name this passkey (optional)", "");
        const passkeyName = providedName ? providedName.trim() : "";

        const credentialJson = serializeCredential(credential);
        const registerResponse = await fetchWithDefaults(apiBase + "/register", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ credentialJson: credentialJson, name: passkeyName })
        });

        if (!registerResponse.ok) {
            let errorBody = null;
            try {
                errorBody = await registerResponse.json();
            } catch {
                errorBody = null;
            }

            if (registerResponse.status === 401) {
                throw new Error((errorBody && errorBody.error) || "Your sign-in session is no longer valid. Please sign in again and retry passkey registration.");
            }

            throw new Error((errorBody && errorBody.error) || ("Passkey registration failed (" + registerResponse.status + ")."));
        }

        showStatus(statusElement, "Passkey registered.", "success");
    }

    async function loadPasskeysAsync(apiBase, tableBody, statusElement) {
        tableBody.innerHTML = '<tr><td colspan="5" class="text-muted">Loading passkeys...</td></tr>';

        const response = await fetchWithDefaults(apiBase + "/list", { method: "GET" });
        if (!response.ok) {
            throw new Error("Failed to load passkeys (" + response.status + ").");
        }

        const passkeys = await response.json();
        if (!Array.isArray(passkeys) || passkeys.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="5" class="text-muted">No passkeys registered.</td></tr>';
            return;
        }

        tableBody.innerHTML = passkeys.map(function (pk) {
            const created = pk.createdAt ? new Date(pk.createdAt).toLocaleString() : "-";
            const flags = [
                pk.isUserVerified ? "UV" : "",
                pk.isBackupEligible ? "BE" : "",
                pk.isBackedUp ? "BU" : ""
            ].filter(Boolean).join(", ") || "-";

            return '<tr>' +
                '<td>' + encodeHtml(pk.name || "Unnamed") + '</td>' +
                '<td>' + encodeHtml(created) + '</td>' +
                '<td>' + encodeHtml(String(pk.signCount || 0)) + '</td>' +
                '<td>' + encodeHtml(flags) + '</td>' +
                '<td><button type="button" class="btn btn-sm btn-outline-danger" data-remove-id="' + encodeHtml(pk.id) + '">Remove</button></td>' +
                '</tr>';
        }).join("");

        clearStatus(statusElement);
    }

    async function removePasskeyAsync(apiBase, id) {
        const response = await fetchWithDefaults(apiBase + "/remove", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ id: id })
        });

        if (!response.ok) {
            let errorBody = null;
            try {
                errorBody = await response.json();
            } catch {
                errorBody = null;
            }
            throw new Error((errorBody && errorBody.error) || ("Failed to remove passkey (" + response.status + ")."));
        }
    }

    async function obtainPasskeyForLogin(apiBase, email, mediation) {
        const optionsResponse = await fetchWithDefaults(apiBase + "/request-options?username=" + encodeURIComponent(email || ""), {
            method: "POST"
        });
        const optionsJson = await parseOptionsJson(optionsResponse);
        const options = PublicKeyCredential.parseRequestOptionsFromJSON(optionsJson);
        return await navigator.credentials.get({ publicKey: options, mediation: mediation });
    }

    async function submitLoginCredential(config, useConditionalMediation) {
        const form = document.querySelector(config.formSelector || "form");
        if (!form) {
            return;
        }

        const emailInput = document.querySelector(config.emailSelector);
        const credentialField = document.querySelector(config.credentialFieldSelector);
        const errorField = document.querySelector(config.errorFieldSelector);

        if (!credentialField || !errorField) {
            return;
        }

        credentialField.value = "";
        errorField.value = "";

        try {
            const mediation = useConditionalMediation ? "conditional" : undefined;
            const credential = await obtainPasskeyForLogin(config.apiBase, emailInput ? emailInput.value : "", mediation);
            if (!credential) {
                throw new Error("No passkey was provided by the authenticator.");
            }

            credentialField.value = serializeCredential(credential);
        } catch (error) {
            if (error && error.name === "AbortError") {
                return;
            }

            if (useConditionalMediation) {
                return;
            }

            errorField.value = (error && error.name === "NotAllowedError")
                ? "No passkey was provided by the authenticator."
                : ((error && error.message) || "Passkey authentication failed.");
        }

        form.submit();
    }

    function initManagePage(config) {
        const apiBase = config.apiBase || "/identity/passkeys";
        const addButton = document.querySelector(config.addButtonSelector || "#addPasskeyBtn");
        const refreshButton = document.querySelector(config.refreshButtonSelector || "#refreshPasskeysBtn");
        const tableBody = document.querySelector(config.tableBodySelector || "#passkeysTableBody");
        const statusElement = document.querySelector(config.statusSelector || "#passkeyStatus");

        if (!addButton || !refreshButton || !tableBody) {
            return;
        }

        addButton.addEventListener("click", async function () {
            clearStatus(statusElement);
            try {
                await addPasskeyAsync(apiBase, statusElement);
                await loadPasskeysAsync(apiBase, tableBody, statusElement);
            } catch (error) {
                showStatus(statusElement, (error && error.message) || "Failed to add passkey.", "danger");
            }
        });

        refreshButton.addEventListener("click", async function () {
            clearStatus(statusElement);
            try {
                await loadPasskeysAsync(apiBase, tableBody, statusElement);
            } catch (error) {
                tableBody.innerHTML = '<tr><td colspan="5" class="text-danger">Failed to load passkeys.</td></tr>';
                showStatus(statusElement, (error && error.message) || "Failed to load passkeys.", "danger");
            }
        });

        tableBody.addEventListener("click", async function (event) {
            const target = event.target;
            if (!(target instanceof HTMLElement) || !target.matches("button[data-remove-id]")) {
                return;
            }

            const id = target.getAttribute("data-remove-id");
            if (!id) {
                return;
            }

            clearStatus(statusElement);
            try {
                await removePasskeyAsync(apiBase, id);
                showStatus(statusElement, "Passkey removed.", "success");
                await loadPasskeysAsync(apiBase, tableBody, statusElement);
            } catch (error) {
                showStatus(statusElement, (error && error.message) || "Failed to remove passkey.", "danger");
            }
        });

        refreshButton.click();
    }

    function initLoginPage(config) {
        const button = document.querySelector(config.buttonSelector || "#passkey-login-submit");
        if (!button) {
            return;
        }

        button.addEventListener("click", async function (event) {
            event.preventDefault();
            await submitLoginCredential(config, false);
        });

        if (!window.PublicKeyCredential || !navigator.credentials || !PublicKeyCredential.isConditionalMediationAvailable) {
            return;
        }

        PublicKeyCredential.isConditionalMediationAvailable()
            .then(function (isAvailable) {
                if (!isAvailable) {
                    return;
                }

                submitLoginCredential(config, true);
            })
            .catch(function () {
                // Ignore conditional mediation probing errors.
            });
    }

    window.AspNetCoreIdentityPasskeys = {
        initManagePage: initManagePage,
        initLoginPage: initLoginPage
    };
})();
