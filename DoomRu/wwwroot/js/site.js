// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
'use strict';

document.addEventListener('DOMContentLoaded', () => {
    initThemeToggle();
    initCookieBanner();
    initPhoneMask();
    initLogin();
    initActiveNav();
    initClickableCards();
    initDiagnosticModal();
});

/* ---------- Тема ---------- */
function initThemeToggle() {
    const btn = document.getElementById('themeToggle');
    if (!btn) return;
    btn.addEventListener('click', () => {
        const cur = document.documentElement.getAttribute('data-theme') || 'dark';
        const next = cur === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', next);
        localStorage.setItem('dr_theme', next);
    });
}

/* ---------- Cookie ---------- */
function initCookieBanner() {
    if (!localStorage.getItem('dr_cookie_accepted')) {
        const b = document.getElementById('cookieBanner');
        if (b) b.style.display = 'block';
    }
}
function acceptCookies() {
    localStorage.setItem('dr_cookie_accepted', '1');
    const b = document.getElementById('cookieBanner');
    if (b) b.style.display = 'none';
}
window.acceptCookies = acceptCookies;

/* ---------- Маска телефона ---------- */
function initPhoneMask() {
    document.querySelectorAll('input[type="tel"]').forEach(input => {
        input.addEventListener('input', () => {
            let d = input.value.replace(/\D/g, '');
            if (!d) { input.value = ''; return; }
            if (d[0] === '8') d = '7' + d.slice(1);
            if (d[0] !== '7') d = '7' + d;
            d = d.slice(0, 11);
            let out = '+7';
            if (d.length > 1) out += ' (' + d.slice(1, 4);
            if (d.length >= 5) out += ') ' + d.slice(4, 7);
            if (d.length >= 8) out += '-' + d.slice(7, 9);
            if (d.length >= 10) out += '-' + d.slice(9, 11);
            input.value = out;
        });
    });
}

/* ---------- Кнопка «Продолжить» на главной ---------- */
function initLogin() {
    const btn = document.querySelector('[data-login]');
    if (!btn) return;
    btn.addEventListener('click', () => {
        const input = document.querySelector('input[type="tel"]');
        const phone = input ? input.value.replace(/\D/g, '') : '';
        if (phone.length < 11) { input && input.focus(); return; }
        // Переходим на страницу входа, подставив номер
        location.href = '/Auth/Login?phone=' + encodeURIComponent(input.value);
    });
}

/* ---------- Активная ссылка ---------- */
function initActiveNav() {
    const path = location.pathname.toLowerCase();
    document.querySelectorAll('.nav__link').forEach(link => {
        const href = (link.getAttribute('href') || '').toLowerCase();
        if (href && path.startsWith(href) && href !== '/') link.classList.add('is-active');
        if (href === '/' && (path === '/' || path === '/index')) link.classList.add('is-active');
    });
}

/* ---------- data-href ---------- */
function initClickableCards() {
    document.querySelectorAll('[data-href]').forEach(el => {
        el.addEventListener('click', e => {
            // не срабатываем на внутренних ссылках/кнопках
            if (e.target.closest('a, button')) return;
            location.href = el.dataset.href;
        });
    });
}

/* ---------- Модалка диагностики ---------- */
function initDiagnosticModal() {
    const modal = document.getElementById('diagModal');
    if (!modal) return;

    const form = modal.querySelector('form');

    document.querySelectorAll('[data-open-modal]').forEach(b =>
        b.addEventListener('click', () => {
            modal.hidden = false;
            document.body.style.overflow = 'hidden';
        })
    );
    modal.querySelectorAll('[data-close-modal]').forEach(b =>
        b.addEventListener('click', () => {
            modal.hidden = true;
            document.body.style.overflow = '';
        })
    );
    document.addEventListener('keydown', e => {
        if (e.key === 'Escape' && !modal.hidden) {
            modal.hidden = true;
            document.body.style.overflow = '';
        }
    });
    form && form.addEventListener('submit', e => {
        e.preventDefault();
        sessionStorage.setItem('answers',
            JSON.stringify(Object.fromEntries(new FormData(form))));
        location.href = '/Result';
    });
}