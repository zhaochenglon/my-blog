(function () {
  "use strict";

  const POSTS = [
    {
      id: 1,
      title: "用原生 JavaScript 构建可维护的单页应用",
      excerpt: "不依赖框架也能写出结构清晰、易于扩展的前端项目。",
      category: "前端",
      tags: ["JavaScript", "架构"],
      date: "2026-05-20",
      thumb: "t1",
      emoji: "JS",
      content: `
        <p>现代前端开发往往默认选择 React 或 Vue，但许多场景下，原生 JavaScript 足以胜任，且体积更小、心智负担更低。</p>
        <h3>模块划分</h3>
        <p>使用 ES Modules 将路由、状态与 UI 拆分为独立文件，每个模块只负责一件事。入口文件只负责组装与启动。</p>
        <h3>简易路由</h3>
        <p>监听 hashchange 或 popstate，根据路径渲染对应视图。配合 History API 可实现无 # 的干净 URL。</p>
        <h3>状态管理</h3>
        <p>用一个轻量 store 对象保存应用状态，通过自定义事件或发布订阅模式通知视图更新，避免全局变量泛滥。</p>
      `,
    },
    {
      id: 2,
      title: "CSS Grid 布局实战：杂志风首页",
      excerpt: "用 Grid 实现不对称、有呼吸感的排版，让博客首页更有设计感。",
      category: "前端",
      tags: ["CSS", "布局"],
      date: "2026-05-12",
      thumb: "t2",
      emoji: "⊞",
      content: `
        <p>Grid 的强大之处在于二维布局：同时控制行与列的对齐与间距。</p>
        <p>杂志风布局常用 <code>grid-template-areas</code> 命名区域，在不同断点切换区域排列，移动端自动变为单列。</p>
      `,
    },
    {
      id: 3,
      title: "晨间写作：三十天习惯养成记录",
      excerpt: "每天清晨写五百字，一个月后发生了什么变化？",
      category: "生活",
      tags: ["习惯", "写作"],
      date: "2026-04-28",
      thumb: "t3",
      emoji: "☀",
      content: `
        <p>清晨的大脑更清晰，干扰更少。坚持三十天后，最大的收获不是字数，而是「开始」变得不再困难。</p>
        <p>建议从固定时间、固定位置和极低目标（如 100 字）起步，降低心理阻力。</p>
      `,
    },
    {
      id: 4,
      title: "TypeScript 类型体操入门",
      excerpt: "从实用角度理解泛型、条件类型与 infer，不再畏惧复杂类型报错。",
      category: "前端",
      tags: ["TypeScript"],
      date: "2026-04-15",
      thumb: "t4",
      emoji: "TS",
      content: `
        <p>类型体操不是为了炫技，而是让编译器帮你捕获更多错误。先从日常工具类型（Partial、Pick、Omit）用起。</p>
      `,
    },
    {
      id: 5,
      title: "在喧嚣中保持专注的几种方法",
      excerpt: "数字时代的注意力是稀缺资源，分享我实践过的几个小技巧。",
      category: "随笔",
      tags: ["思考", "效率"],
      date: "2026-03-30",
      thumb: "t5",
      emoji: "◎",
      content: `
        <p>手机灰度、番茄钟、以及「单任务清单」——简单的方法往往最有效。关键是找到适合自己的节奏并坚持。</p>
      `,
    },
    {
      id: 6,
      title: "徒步武功山：云海与星空",
      excerpt: "周末两天的轻徒步行程记录，含路线与装备清单。",
      category: "生活",
      tags: ["旅行", "户外"],
      date: "2026-03-08",
      thumb: "t6",
      emoji: "⛰",
      content: `
        <p>武功山的高山草甸在春夏之交最为迷人。建议携带防风外套与充足饮水，山顶温差较大。</p>
        <p>若时间充裕，可在金顶等待日落与星空，记得头灯与保暖层。</p>
      `,
    },
  ];

  const PAGE_SIZE = 3;
  let currentFilter = "all";
  let visibleCount = PAGE_SIZE;
  let searchQuery = "";

  const $ = (sel, root = document) => root.querySelector(sel);
  const $$ = (sel, root = document) => [...root.querySelectorAll(sel)];

  const postsGrid = $("#postsGrid");
  const postCount = $("#postCount");
  const loadMoreBtn = $("#loadMore");
  const postModal = $("#postModal");
  const modalTitle = $("#modalTitle");
  const modalMeta = $("#modalMeta");
  const modalBody = $("#modalBody");
  const toast = $("#toast");

  function formatDate(iso) {
    const d = new Date(iso);
    return d.toLocaleDateString("zh-CN", {
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  }

  function getFilteredPosts() {
    return POSTS.filter((p) => {
      const matchCat =
        currentFilter === "all" || p.category === currentFilter;
      const q = searchQuery.trim().toLowerCase();
      const matchSearch =
        !q ||
        p.title.toLowerCase().includes(q) ||
        p.excerpt.toLowerCase().includes(q) ||
        p.tags.some((t) => t.toLowerCase().includes(q)) ||
        p.category.toLowerCase().includes(q);
      return matchCat && matchSearch;
    });
  }

  function renderPosts() {
    const filtered = getFilteredPosts();
    const toShow = filtered.slice(0, visibleCount);

    postsGrid.innerHTML = toShow
      .map(
        (post, i) => `
      <article class="post-card" data-id="${post.id}" style="animation-delay:${i * 0.06}s">
        <div class="post-thumb ${post.thumb}">${post.emoji}</div>
        <div class="post-body">
          <div class="post-meta">
            <span class="tag">${post.category}</span>
            <time datetime="${post.date}">${formatDate(post.date)}</time>
          </div>
          <h3>${post.title}</h3>
          <p class="post-excerpt">${post.excerpt}</p>
          <div class="post-tags">${post.tags.map((t) => `<span>${t}</span>`).join("")}</div>
        </div>
      </article>
    `
      )
      .join("");

    postCount.textContent = `共 ${filtered.length} 篇文章`;
    loadMoreBtn.classList.toggle("hidden", visibleCount >= filtered.length);

    $$(".post-card", postsGrid).forEach((card) => {
      card.addEventListener("click", () => openPost(Number(card.dataset.id)));
    });
  }

  function openPost(id) {
    const post = POSTS.find((p) => p.id === id);
    if (!post) return;
    modalMeta.innerHTML = `<span class="tag">${post.category}</span><time>${formatDate(post.date)}</time>`;
    modalTitle.textContent = post.title;
    modalBody.innerHTML = post.content;
    postModal.showModal();
  }

  function showToast(message) {
    toast.textContent = message;
    toast.classList.add("show");
    clearTimeout(showToast._timer);
    showToast._timer = setTimeout(() => toast.classList.remove("show"), 2800);
  }

  function initTheme() {
    const saved = localStorage.getItem("blog-theme");
    const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
    const theme = saved || (prefersDark ? "dark" : "light");
    document.documentElement.setAttribute("data-theme", theme);
  }

  function toggleTheme() {
    const current = document.documentElement.getAttribute("data-theme");
    const next = current === "dark" ? "light" : "dark";
    document.documentElement.setAttribute("data-theme", next);
    localStorage.setItem("blog-theme", next);
  }

  function initStats() {
    $$(".stat-num").forEach((el) => {
      const target = Number(el.dataset.count);
      let current = 0;
      const step = Math.ceil(target / 30);
      const timer = setInterval(() => {
        current += step;
        if (current >= target) {
          current = target;
          clearInterval(timer);
        }
        el.textContent = current;
      }, 40);
    });
  }

  function initTagCloud() {
    const tags = [...new Set(POSTS.flatMap((p) => p.tags))];
    const cloud = $("#tagCloud");
    cloud.innerHTML = tags
      .map((t) => `<button type="button" data-tag="${t}">${t}</button>`)
      .join("");
    $$("button", cloud).forEach((btn) => {
      btn.addEventListener("click", () => {
        searchQuery = btn.dataset.tag;
        visibleCount = PAGE_SIZE;
        $("#searchInput").value = searchQuery;
        renderPosts();
        document.getElementById("posts").scrollIntoView({ behavior: "smooth" });
      });
    });
  }

  function initNav() {
    const nav = $("#siteNav");
    const toggle = $("#navToggle");

    toggle.addEventListener("click", () => {
      const open = nav.classList.toggle("open");
      toggle.classList.toggle("open", open);
      toggle.setAttribute("aria-expanded", String(open));
    });

    $$(".nav-link").forEach((link) => {
      link.addEventListener("click", () => {
        nav.classList.remove("open");
        toggle.classList.remove("open");
        toggle.setAttribute("aria-expanded", "false");
        $$(".nav-link").forEach((l) => l.classList.remove("active"));
        link.classList.add("active");
      });
    });

    const sections = $$("section[id]");
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            const id = entry.target.id;
            $$(".nav-link").forEach((l) => {
              l.classList.toggle("active", l.getAttribute("href") === `#${id}`);
            });
          }
        });
      },
      { rootMargin: "-40% 0px -50% 0px" }
    );
    sections.forEach((s) => observer.observe(s));
  }

  function initSearch() {
    const overlay = $("#searchOverlay");
    const input = $("#searchInput");

    $("#searchBtn").addEventListener("click", () => {
      overlay.hidden = false;
      input.focus();
    });

    $("#searchClose").addEventListener("click", closeSearch);
    overlay.addEventListener("click", (e) => {
      if (e.target === overlay) closeSearch();
    });

    input.addEventListener("input", () => {
      searchQuery = input.value;
      visibleCount = PAGE_SIZE;
      renderPosts();
    });

    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape" && !overlay.hidden) closeSearch();
      if ((e.ctrlKey || e.metaKey) && e.key === "k") {
        e.preventDefault();
        overlay.hidden = false;
        input.focus();
      }
    });

    function closeSearch() {
      overlay.hidden = true;
    }
  }

  function initFilters() {
    $$(".filter-tab").forEach((tab) => {
      tab.addEventListener("click", () => {
        $$(".filter-tab").forEach((t) => t.classList.remove("active"));
        tab.classList.add("active");
        currentFilter = tab.dataset.filter;
        visibleCount = PAGE_SIZE;
        renderPosts();
      });
    });
  }

  function initLoadMore() {
    loadMoreBtn.addEventListener("click", () => {
      visibleCount += PAGE_SIZE;
      renderPosts();
    });
  }

  function initModal() {
    $("#modalClose").addEventListener("click", () => postModal.close());
    postModal.addEventListener("click", (e) => {
      if (e.target === postModal) postModal.close();
    });

    document.addEventListener("click", (e) => {
      const link = e.target.closest("[data-post]");
      if (link) {
        e.preventDefault();
        openPost(Number(link.dataset.post));
      }
    });
  }

  function initForms() {
    $("#subscribeForm").addEventListener("submit", (e) => {
      e.preventDefault();
      e.target.reset();
      showToast("感谢订阅！我们会把更新发到你的邮箱。");
    });

    $("#contactForm").addEventListener("submit", (e) => {
      e.preventDefault();
      e.target.reset();
      showToast("消息已发送，我会尽快回复你。");
    });
  }

  $("#themeToggle").addEventListener("click", toggleTheme);
  $("#year").textContent = new Date().getFullYear();

  initTheme();
  initStats();
  initTagCloud();
  initNav();
  initSearch();
  initFilters();
  initLoadMore();
  initModal();
  initForms();
  renderPosts();
})();
