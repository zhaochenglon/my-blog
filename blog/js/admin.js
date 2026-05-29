(function () {
  "use strict";

  const TOKEN_KEY = "blog_admin_token";
  const USER_KEY = "blog_admin_user";
  const PAGE_SIZE = 15;

  const loginSection = document.getElementById("loginSection");
  const dashboardSection = document.getElementById("dashboardSection");
  const loginForm = document.getElementById("loginForm");
  const loginBtn = document.getElementById("loginBtn");
  const logoutBtn = document.getElementById("logoutBtn");
  const adminHeaderActions = document.getElementById("adminHeaderActions");
  const adminUserLabel = document.getElementById("adminUserLabel");
  const messagesBody = document.getElementById("messagesBody");
  const emptyState = document.getElementById("emptyState");
  const messageSummary = document.getElementById("messageSummary");
  const pagination = document.getElementById("pagination");
  const pageInfo = document.getElementById("pageInfo");
  const prevPageBtn = document.getElementById("prevPageBtn");
  const nextPageBtn = document.getElementById("nextPageBtn");
  const refreshBtn = document.getElementById("refreshBtn");
  const deleteBtn = document.getElementById("deleteBtn");
  const selectAllCheckbox = document.getElementById("selectAllCheckbox");
  const toast = document.getElementById("toast");

  let currentPage = 1;
  let totalCount = 0;
  let currentItems = [];
  const selectedIds = new Set();

  function getApiBaseUrl() {
    const configured = window.BLOG_CONFIG?.apiBaseUrl;
    if (configured) return configured.replace(/\/$/, "");
    return window.location.origin;
  }

  function getToken() {
    return sessionStorage.getItem(TOKEN_KEY);
  }

  function showToast(message, isError) {
    toast.textContent = message;
    toast.classList.toggle("toast-error", Boolean(isError));
    toast.classList.add("show");
    clearTimeout(showToast._timer);
    showToast._timer = setTimeout(() => toast.classList.remove("show"), 3200);
  }

  function escapeHtml(text) {
    return String(text)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function formatDate(iso) {
    let value = String(iso || "");
    if (value && !value.endsWith("Z") && !value.includes("+")) {
      value += "Z";
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "-";

    const parts = new Intl.DateTimeFormat("zh-CN", {
      timeZone: "Asia/Shanghai",
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
      hour12: false,
    }).formatToParts(date);

    const get = (type) => parts.find((p) => p.type === type)?.value || "";
    return `${get("year")}-${get("month")}-${get("day")} ${get("hour")}:${get("minute")}:${get("second")}`;
  }

  function truncate(text, max) {
    const value = String(text || "");
    return value.length > max ? `${value.slice(0, max)}…` : value;
  }

  function showLogin() {
    loginSection.hidden = false;
    dashboardSection.hidden = true;
    adminHeaderActions.hidden = true;
  }

  function showDashboard(username) {
    loginSection.hidden = true;
    dashboardSection.hidden = false;
    adminHeaderActions.hidden = false;
    adminUserLabel.textContent = username || "管理员";
  }

  async function apiFetch(path, options) {
    const headers = Object.assign({ "Content-Type": "application/json" }, options?.headers || {});
    const token = getToken();
    if (token) headers.Authorization = `Bearer ${token}`;

    const res = await fetch(`${getApiBaseUrl()}${path}`, Object.assign({}, options, { headers }));
    if (res.status === 401) {
      sessionStorage.removeItem(TOKEN_KEY);
      sessionStorage.removeItem(USER_KEY);
      showLogin();
      throw new Error("登录已过期，请重新登录");
    }
    return res;
  }

  async function handleLogin(event) {
    event.preventDefault();
    loginBtn.disabled = true;
    loginBtn.textContent = "登录中…";

    const username = loginForm.username.value.trim();
    const password = loginForm.password.value;

    try {
      const res = await fetch(`${getApiBaseUrl()}/api/admin/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password }),
      });

      if (!res.ok) {
        showToast("账号或密码错误", true);
        return;
      }

      const data = await res.json();
      sessionStorage.setItem(TOKEN_KEY, data.token);
      sessionStorage.setItem(USER_KEY, username);
      loginForm.reset();
      showDashboard(username);
      await loadMessages(1);
      showToast("登录成功");
    } catch {
      showToast("网络错误，请稍后重试", true);
    } finally {
      loginBtn.disabled = false;
      loginBtn.textContent = "登录";
    }
  }

  function updateDeleteButton() {
    deleteBtn.disabled = selectedIds.size === 0;
    deleteBtn.textContent =
      selectedIds.size > 0 ? `删除选中 (${selectedIds.size})` : "删除选中";
  }

  function updateSelectAllCheckbox() {
    if (currentItems.length === 0) {
      selectAllCheckbox.checked = false;
      selectAllCheckbox.indeterminate = false;
      return;
    }

    const selectedOnPage = currentItems.filter((item) => selectedIds.has(item.id)).length;
    selectAllCheckbox.checked = selectedOnPage === currentItems.length;
    selectAllCheckbox.indeterminate =
      selectedOnPage > 0 && selectedOnPage < currentItems.length;
  }

  function renderMessages(items) {
    currentItems = items;
    messagesBody.innerHTML = items
      .map(
        (item) => `
      <tr data-id="${item.id}">
        <td class="admin-col-check">
          <input type="checkbox" class="row-checkbox" data-id="${item.id}" aria-label="选择留言 ${item.id}" ${selectedIds.has(item.id) ? "checked" : ""} />
        </td>
        <td>${item.id}</td>
        <td>${escapeHtml(item.name)}</td>
        <td><a href="mailto:${escapeHtml(item.email)}">${escapeHtml(item.email)}</a></td>
        <td class="admin-content-cell" title="${escapeHtml(item.content)}">${escapeHtml(truncate(item.content, 80))}</td>
        <td>${formatDate(item.createdAt)}</td>
      </tr>`
      )
      .join("");

    const hasItems = items.length > 0;
    emptyState.hidden = hasItems;
    document.querySelector(".admin-table-wrap").hidden = !hasItems;
    updateSelectAllCheckbox();
    updateDeleteButton();
  }

  function updatePagination() {
    const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
    pagination.hidden = totalCount <= PAGE_SIZE;
    pageInfo.textContent = `第 ${currentPage} / ${totalPages} 页`;
    prevPageBtn.disabled = currentPage <= 1;
    nextPageBtn.disabled = currentPage >= totalPages;
    messageSummary.textContent = `共 ${totalCount} 条留言`;
  }

  async function loadMessages(page) {
    currentPage = page;
    refreshBtn.disabled = true;
    refreshBtn.textContent = "加载中…";

    try {
      const res = await apiFetch(`/api/messages?page=${page}&pageSize=${PAGE_SIZE}`);
      if (!res.ok) throw new Error("加载失败");

      const data = await res.json();
      totalCount = data.totalCount ?? 0;
      renderMessages(data.items || []);
      updatePagination();
    } catch (err) {
      showToast(err.message || "加载留言失败", true);
    } finally {
      refreshBtn.disabled = false;
      refreshBtn.textContent = "刷新";
    }
  }

  async function deleteSelected() {
    const ids = Array.from(selectedIds);
    if (ids.length === 0) return;

    const confirmed = window.confirm(`确定删除选中的 ${ids.length} 条留言吗？此操作不可恢复。`);
    if (!confirmed) return;

    deleteBtn.disabled = true;
    deleteBtn.textContent = "删除中…";

    try {
      const results = await Promise.all(
        ids.map((id) => apiFetch(`/api/messages/${id}`, { method: "DELETE" }))
      );

      const failed = results.filter((res) => !res.ok && res.status !== 404).length;
      if (failed > 0) {
        showToast(`有 ${failed} 条删除失败`, true);
      } else {
        showToast(`已删除 ${ids.length} 条留言`);
      }

      ids.forEach((id) => selectedIds.delete(id));

      const totalPages = Math.max(1, Math.ceil((totalCount - ids.length) / PAGE_SIZE));
      const nextPage = currentPage > totalPages ? totalPages : currentPage;
      await loadMessages(nextPage);
    } catch (err) {
      showToast(err.message || "删除失败", true);
    } finally {
      updateDeleteButton();
    }
  }

  function handleLogout() {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(USER_KEY);
    showLogin();
    showToast("已退出登录");
  }

  loginForm.addEventListener("submit", handleLogin);
  logoutBtn.addEventListener("click", handleLogout);
  refreshBtn.addEventListener("click", () => loadMessages(currentPage));
  deleteBtn.addEventListener("click", deleteSelected);

  selectAllCheckbox.addEventListener("change", () => {
    if (selectAllCheckbox.checked) {
      currentItems.forEach((item) => selectedIds.add(item.id));
    } else {
      currentItems.forEach((item) => selectedIds.delete(item.id));
    }
    renderMessages(currentItems);
  });

  messagesBody.addEventListener("change", (event) => {
    const checkbox = event.target.closest(".row-checkbox");
    if (!checkbox) return;

    const id = Number(checkbox.dataset.id);
    if (checkbox.checked) {
      selectedIds.add(id);
    } else {
      selectedIds.delete(id);
    }
    updateSelectAllCheckbox();
    updateDeleteButton();
  });

  prevPageBtn.addEventListener("click", () => {
    if (currentPage > 1) loadMessages(currentPage - 1);
  });
  nextPageBtn.addEventListener("click", () => {
    const totalPages = Math.ceil(totalCount / PAGE_SIZE);
    if (currentPage < totalPages) loadMessages(currentPage + 1);
  });

  const savedUser = sessionStorage.getItem(USER_KEY);
  if (getToken() && savedUser) {
    showDashboard(savedUser);
    loadMessages(1);
  } else {
    showLogin();
  }
})();
