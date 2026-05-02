/**
 * navbar.js — Include this on every HTML page.
 * Automatically shows:
 *   - "Login" button when logged out
 *   - User avatar + name + dropdown menu when logged in
 *
 * FIXED: Listens to storage events so navbar updates instantly after login
 *        without needing a page refresh.
 *
 * Usage: Add this just before </body> on every page:
 *   <script src="navbar.js"></script>
 */

(function () {
  function renderNav() {
    const token    = localStorage.getItem("token");
    const fullName = localStorage.getItem("fullName");
    const email    = localStorage.getItem("email");

    const nav = document.querySelector("nav");
    if (!nav) return;

    const existing = nav.querySelector(".nav-auth");
    if (existing) existing.remove();

    const authDiv = document.createElement("div");
    authDiv.className = "nav-auth";

    if (token && fullName) {
      const initials = fullName.split(" ").map(n => n[0]).join("").toUpperCase().slice(0, 2);

      authDiv.innerHTML = `
        <div class="nav-user" id="navUserBtn">
          <div class="nav-avatar">${initials}</div>
          <span class="nav-username">${fullName.split(" ")[0]}</span>
          <i class="fa fa-chevron-down nav-chevron"></i>
        </div>
        <div class="nav-dropdown" id="navDropdown">
          <div class="nav-dropdown-header">
            <div class="nav-avatar-lg">${initials}</div>
            <div>
              <div class="nav-drop-name">${fullName}</div>
              <div class="nav-drop-email">${email || ""}</div>
            </div>
          </div>
          <hr class="nav-divider">
          <a href="homepage.html" class="nav-drop-item">
            <i class="fa fa-user"></i> My Profile
          </a>
          <a href="#" class="nav-drop-item nav-drop-logout" id="navLogoutBtn">
            <i class="fa fa-sign-out"></i> Logout
          </a>
        </div>
      `;
    } else {
      authDiv.innerHTML = `
        <a href="contact.html" class="nav-login-btn">
          <i class="fa fa-user"></i> Login
        </a>
      `;
    }

    nav.appendChild(authDiv);

    // Dropdown toggle
    const userBtn  = document.getElementById("navUserBtn");
    const dropdown = document.getElementById("navDropdown");
    const logoutBtn= document.getElementById("navLogoutBtn");

    if (userBtn && dropdown) {
      userBtn.addEventListener("click", function (e) {
        e.stopPropagation();
        dropdown.classList.toggle("open");
        userBtn.classList.toggle("active");
      });
      document.addEventListener("click", function () {
        dropdown.classList.remove("open");
        if (userBtn) userBtn.classList.remove("active");
      });
    }

    if (logoutBtn) {
      logoutBtn.addEventListener("click", function (e) {
        e.preventDefault();
        localStorage.removeItem("token");
        localStorage.removeItem("fullName");
        localStorage.removeItem("email");
        renderNav(); // re-render in place — no reload needed
        // If there's a page-level renderAuthState (contact page), call it too
        if (typeof renderAuthState === "function") renderAuthState();
      });
    }
  }

  // Initial render
  renderNav();

  // ── KEY FIX: listen for login/logout from any tab or same-page storage writes ──
  window.addEventListener("storage", function (e) {
    if (["token", "fullName", "email"].includes(e.key)) {
      renderNav();
    }
  });

  // Also expose a global so contact.html can trigger it after in-page login
  window._navbarRefresh = renderNav;

  // ── Inject styles ─────────────────────────────────────────────────────────────
  if (!document.getElementById("nav-auth-styles")) {
    const style = document.createElement("style");
    style.id = "nav-auth-styles";
    style.textContent = `
      nav {
        display: flex;
        align-items: center;
        justify-content: space-between;
        flex-wrap: wrap;
        position: relative;
      }

      .nav-auth {
        margin-left: auto;
        position: relative;
        z-index: 1000;
      }

      /* ── Login button ── */
      .nav-login-btn {
        display: inline-flex;
        align-items: center;
        gap: 7px;
        padding: 8px 20px;
        background: #fff;
        color: #333;
        border-radius: 25px;
        font-weight: 600;
        font-size: 14px;
        text-decoration: none;
        box-shadow: 0 2px 8px rgba(0,0,0,0.15);
        transition: all 0.25s ease;
        white-space: nowrap;
      }
      .nav-login-btn:hover {
        background: #c9a84c;
        color: #fff;
        transform: translateY(-1px);
        box-shadow: 0 4px 14px rgba(201,168,76,0.5);
      }

      /* ── User pill ── */
      .nav-user {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        padding: 5px 14px 5px 5px;
        background: rgba(255,255,255,0.15);
        border-radius: 30px;
        cursor: pointer;
        transition: background 0.2s;
        user-select: none;
        border: 1px solid rgba(255,255,255,0.3);
      }
      .nav-user:hover, .nav-user.active {
        background: rgba(255,255,255,0.25);
      }

      .nav-avatar {
        width: 34px; height: 34px;
        border-radius: 50%;
        background: linear-gradient(135deg, #c9a84c, #a07830);
        color: #fff;
        display: flex; align-items: center; justify-content: center;
        font-weight: 700; font-size: 13px;
        flex-shrink: 0;
        box-shadow: 0 2px 6px rgba(201,168,76,0.5);
      }
      .nav-username {
        color: #fff;
        font-weight: 600; font-size: 14px;
        max-width: 100px;
        overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
      }
      .nav-chevron {
        color: rgba(255,255,255,0.8); font-size: 11px;
        transition: transform 0.2s;
      }
      .nav-user.active .nav-chevron { transform: rotate(180deg); }

      /* ── Dropdown ── */
      .nav-dropdown {
        display: none;
        position: absolute;
        top: calc(100% + 10px); right: 0;
        width: 240px;
        background: #fff;
        border-radius: 12px;
        box-shadow: 0 8px 30px rgba(0,0,0,0.15);
        overflow: hidden;
        animation: dropFade 0.2s ease;
      }
      .nav-dropdown.open { display: block; }

      @keyframes dropFade {
        from { opacity: 0; transform: translateY(-6px); }
        to   { opacity: 1; transform: translateY(0); }
      }

      .nav-dropdown-header {
        display: flex; align-items: center; gap: 12px;
        padding: 16px; background: #fafafa;
      }
      .nav-avatar-lg {
        width: 44px; height: 44px; border-radius: 50%;
        background: linear-gradient(135deg, #c9a84c, #a07830);
        color: #fff;
        display: flex; align-items: center; justify-content: center;
        font-weight: 700; font-size: 16px; flex-shrink: 0;
      }
      .nav-drop-name  { font-weight: 700; font-size: 14px; color: #111; }
      .nav-drop-email { font-size: 12px; color: #888; margin-top: 2px; word-break: break-all; }

      .nav-divider { border: none; border-top: 1px solid #f0f0f0; margin: 0; }

      .nav-drop-item {
        display: flex; align-items: center; gap: 10px;
        padding: 12px 16px; color: #333;
        text-decoration: none; font-size: 14px;
        transition: background 0.15s;
      }
      .nav-drop-item:hover { background: #f5f5f5; color: #111; }
      .nav-drop-item i { width: 16px; color: #888; }
      .nav-drop-logout { color: #dc2626; }
      .nav-drop-logout i { color: #dc2626; }
      .nav-drop-logout:hover { background: #fef2f2; }
    `;
    document.head.appendChild(style);
  }
})();