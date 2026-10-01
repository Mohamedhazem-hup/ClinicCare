document.addEventListener("DOMContentLoaded", function () {
    document.addEventListener("click", function (event) {
        const addBillButton = event.target.closest("[data-add-bill-item]");
        if (addBillButton) {
            const form = addBillButton.closest("form");
            const list = form?.querySelector("[data-bill-item-list]");
            const template = document.getElementById("bill-item-template");
            if (!list || !template) return;

            const index = list.querySelectorAll("[data-bill-item-row]").length;
            const row = template.content.cloneNode(true);
            row.querySelectorAll("[name]").forEach(function (input) {
                input.name = input.name.replace("__index__", index);
            });
            list.appendChild(row);
            return;
        }

        const removeBillButton = event.target.closest("[data-remove-bill-item]");
        if (removeBillButton) {
            const list = removeBillButton.closest("[data-bill-item-list]");
            removeBillButton.closest("[data-bill-item-row]")?.remove();
            list?.querySelectorAll("[data-bill-item-row]").forEach(function (row, index) {
                row.querySelectorAll("[name]").forEach(function (input) {
                    input.name = input.name.replace(/Items\[\d+\]/, "Items[" + index + "]");
                });
            });
            return;
        }

        const addButton = event.target.closest("[data-add-prescription-item]");
        if (addButton) {
            const form = addButton.closest("form");
            const container = form?.querySelector("[data-prescription-items]");
            const list = container?.querySelector("[data-prescription-item-list]");
            const template = document.getElementById("prescription-item-template");
            if (!list || !template) return;

            const index = list.querySelectorAll("[data-prescription-item-row]").length;
            const row = template.content.cloneNode(true);
            row.querySelectorAll("[name]").forEach(function (input) {
                input.name = input.name.replace("__index__", index);
            });
            list.appendChild(row);
            return;
        }

        const removeButton = event.target.closest("[data-remove-prescription-item]");
        if (removeButton) {
            const list = removeButton.closest("[data-prescription-item-list]");
            removeButton.closest("[data-prescription-item-row]")?.remove();
            list?.querySelectorAll("[data-prescription-item-row]").forEach(function (row, index) {
                row.querySelectorAll("[name]").forEach(function (input) {
                    input.name = input.name.replace(/Items\[\d+\]/, "Items[" + index + "]");
                });
            });
        }
    });

    document.querySelectorAll("section").forEach(function (section) {
        const heading = section.querySelector("h2, h3");
        if (!heading || !heading.textContent.trim().startsWith("Use another service to")) return;

        const column = section.parentElement;
        if (column && Array.from(column.classList).some(function (name) { return name.startsWith("col-"); })) {
            column.remove();
        } else {
            section.remove();
        }
    });

    if (window.location.pathname.toLowerCase() === "/identity/account/login") {
        const loginForm = document.querySelector("form#account");
        if (loginForm) {
            const demoAccounts = document.createElement("section");
            demoAccounts.className = "demo-accounts";
            demoAccounts.innerHTML = `
                <h2>Demo Accounts</h2>
                <div class="table-responsive">
                    <table class="demo-accounts-table">
                        <thead><tr><th>Role</th><th>Email</th><th>Password</th></tr></thead>
                        <tbody>
                            <tr><td>Admin</td><td>admin@clinic.com</td><td>Admin123</td></tr>
                            <tr><td>Receptionist</td><td>reception@clinic.com</td><td>Reception123</td></tr>
                            <tr><td>Doctor</td><td>doctor@clinic.com</td><td>Doctor123</td></tr>
                            <tr><td>Patient</td><td>patient@clinic.com</td><td>Patient123</td></tr>
                        </tbody>
                    </table>
                </div>`;
            loginForm.insertAdjacentElement("afterend", demoAccounts);
        }
    }

    const sidebar = document.getElementById("sidebar");
    const toggleButton = document.getElementById("sidebarToggle");
    const closeButton = document.getElementById("sidebarClose");

    if (toggleButton && sidebar) {
        toggleButton.addEventListener("click", function () { sidebar.classList.add("open"); });
    }
    if (closeButton && sidebar) {
        closeButton.addEventListener("click", function () { sidebar.classList.remove("open"); });
    }

    document.addEventListener("click", function (event) {
        if (!sidebar || !sidebar.classList.contains("open")) return;
        const clickedInsideSidebar = sidebar.contains(event.target);
        const clickedToggle = toggleButton && toggleButton.contains(event.target);
        if (!clickedInsideSidebar && !clickedToggle) sidebar.classList.remove("open");
    });

    document.addEventListener("keydown", function (event) {
        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
            event.preventDefault();
            const searchInput = document.querySelector(".topbar-search input");
            if (searchInput) searchInput.focus();
        }
    });

    const searchInput = document.querySelector(".topbar-search input");
    if (searchInput) {
        searchInput.addEventListener("input", function () {
            const value = this.value.toLowerCase().trim();
            const tables = document.querySelectorAll(".table tbody");
            tables.forEach(function (tbody) {
                const rows = tbody.querySelectorAll("tr");
                rows.forEach(function (row) {
                    const text = row.innerText.toLowerCase();
                    row.style.display = !value || text.includes(value) ? "" : "none";
                });
            });
        });
    }

    document.querySelectorAll('input[type="password"]').forEach(function (input) {
        let container = input.parentElement;
        if (container.classList.contains("form-floating")) {
            container.classList.add("password-toggle-container");
        } else {
            const wrapper = document.createElement("div");
            wrapper.className = "password-toggle-wrapper";
            container.insertBefore(wrapper, input);
            wrapper.appendChild(input);
            container = wrapper;
        }

        const toggle = document.createElement("button");
        toggle.type = "button";
        toggle.className = "password-toggle-button";
        toggle.setAttribute("aria-label", "Show password");
        toggle.setAttribute("title", "Show password");
        toggle.innerHTML = '<i class="fa-solid fa-eye" aria-hidden="true"></i>';
        container.appendChild(toggle);

        toggle.addEventListener("click", function () {
            const shouldShow = input.type === "password";
            input.type = shouldShow ? "text" : "password";
            toggle.setAttribute("aria-label", shouldShow ? "Hide password" : "Show password");
            toggle.setAttribute("title", shouldShow ? "Hide password" : "Show password");
            toggle.innerHTML = shouldShow
                ? '<i class="fa-solid fa-eye-slash" aria-hidden="true"></i>'
                : '<i class="fa-solid fa-eye" aria-hidden="true"></i>';
        });
    });
});
