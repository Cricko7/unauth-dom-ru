'use strict';

document.addEventListener('DOMContentLoaded', () => {
  initPhoneMask();
  initLogin();
  initActiveNav();
  initClickableCards();
  initDiagnosticModal();
});

/* Маска телефона */
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

/* Кнопка «Продолжить» */
function initLogin() {
  const btn = document.querySelector('[data-login]');
  if (!btn) return;
  btn.addEventListener('click', () => {
    const input = document.querySelector('input[type="tel"]');
    if (input && input.value.replace(/\D/g, '').length < 11) {
      input.focus();
      return;
    }
    location.href = location.pathname;
  });
}

/* Активная ссылка */
function initActiveNav() {
  const page = location.pathname.split('/').pop() || 'index.html';
  document.querySelectorAll('.nav__link').forEach(link => {
    if (link.getAttribute('href') === page) link.classList.add('is-active');
  });
}

/* data-href */
function initClickableCards() {
  document.querySelectorAll('[data-href]').forEach(el => {
    el.addEventListener('click', () => {
      location.href = el.dataset.href;
    });
  });
}

/* Модальное окно */
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
  form.addEventListener('submit', e => {
    e.preventDefault();
    sessionStorage.setItem('answers',
      JSON.stringify(Object.fromEntries(new FormData(form))));
    location.href = 'result.html';
  });
}