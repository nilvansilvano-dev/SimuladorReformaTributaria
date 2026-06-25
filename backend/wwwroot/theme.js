'use strict';

(function () {
  const STORAGE_KEY = 'bg_theme';

  const ICON_MOON = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" style="vertical-align:-2px"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>';
  const ICON_SUN  = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" style="vertical-align:-2px"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/></svg>';

  function aplicar(tema) {
    document.documentElement.setAttribute('data-theme', tema);
    const btn = document.getElementById('theme-toggle');
    if (btn) btn.innerHTML = tema === 'dark'
      ? ICON_SUN  + ' <span style="font-size:.78rem">Claro</span>'
      : ICON_MOON + ' <span style="font-size:.78rem">Escuro</span>';
  }

  const salvo = localStorage.getItem(STORAGE_KEY) || 'light';
  aplicar(salvo);

  window.toggleTheme = function () {
    const atual = document.documentElement.getAttribute('data-theme') || 'light';
    const novo  = atual === 'dark' ? 'light' : 'dark';
    localStorage.setItem(STORAGE_KEY, novo);
    aplicar(novo);
  };
})();
