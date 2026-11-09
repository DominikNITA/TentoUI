/**
 * TentoUI Core Interop Layer
 * Provides focus trapping, scroll locking, and accessible boundary management.
 */
const tentoModal = {
  activeModals: [],

  open(container) {
    if (!container) return;

    // Save previous active element to restore later
    const previousActiveElement = document.activeElement;
    this.activeModals.push({ container, previousActiveElement });

    // Lock body scrolling
    document.body.style.overflow = 'hidden';

    const FOCUSABLE_SELECTOR =
      'button:not([disabled]):not([tabindex="-1"]), [href]:not([tabindex="-1"]), input:not([disabled]):not([tabindex="-1"]), select:not([disabled]):not([tabindex="-1"]), textarea:not([disabled]):not([tabindex="-1"]), [tabindex]:not([tabindex="-1"])';

    // Trap focus inside modal
    const focusableElements = container.querySelectorAll(FOCUSABLE_SELECTOR);

    if (focusableElements.length > 0) {
      focusableElements[0].focus();
    } else {
      container.focus();
    }

    const keyHandler = (e) => {
      if (e.key !== 'Tab') return;

      const currentFocusables = container.querySelectorAll(FOCUSABLE_SELECTOR);
      if (currentFocusables.length === 0) {
        e.preventDefault();
        container.focus();
        return;
      }

      const first = currentFocusables[0];
      const last = currentFocusables[currentFocusables.length - 1];

      if (e.shiftKey && document.activeElement === first) {
        last.focus();
        e.preventDefault();
      } else if (!e.shiftKey && document.activeElement === last) {
        first.focus();
        e.preventDefault();
      }
    };

    container._keyHandler = keyHandler;
    container.addEventListener('keydown', keyHandler);
  },

  close(container) {
    if (!container) return;

    if (container._keyHandler) {
      container.removeEventListener('keydown', container._keyHandler);
      delete container._keyHandler;
    }

    const index = this.activeModals.findIndex((m) => m.container === container);
    if (index !== -1) {
      const entry = this.activeModals.splice(index, 1)[0];
      if (entry.previousActiveElement && typeof entry.previousActiveElement.focus === 'function') {
        entry.previousActiveElement.focus();
      }
    }

    // If no more modals are open, unlock body scrolling
    if (this.activeModals.length === 0) {
      document.body.style.overflow = '';
    }
  }
};

const tentoClipboard = {
  copy(text) {
    if (navigator && navigator.clipboard) {
      return navigator.clipboard.writeText(text);
    }
    return Promise.resolve();
  }
};

const tentoTour = {
  _dotnetHelper: null,
  _targetSelector: null,
  _handler: null,

  getTargetRect(selector) {
    if (!selector) return null;
    try {
      const el = document.querySelector(selector);
      if (!el) return null;
      const rect = el.getBoundingClientRect();
      const inView =
        rect.top >= 20 &&
        rect.bottom <= window.innerHeight - 20 &&
        rect.left >= 20 &&
        rect.right <= window.innerWidth - 20;
      if (!inView) {
        el.scrollIntoView({ behavior: 'auto', block: 'center', inline: 'nearest' });
      }
      const updated = el.getBoundingClientRect();
      return {
        left: Math.round(updated.left),
        top: Math.round(updated.top),
        right: Math.round(updated.right),
        bottom: Math.round(updated.bottom),
        width: Math.round(updated.width),
        height: Math.round(updated.height),
        windowWidth: window.innerWidth,
        windowHeight: window.innerHeight
      };
    } catch (e) {
      return null;
    }
  },

  startTracking(selector, dotnetHelper) {
    this._targetSelector = selector;
    this._dotnetHelper = dotnetHelper;
    if (!this._handler) {
      this._handler = () => {
        if (this._dotnetHelper && this._targetSelector) {
          const r = this.getTargetRect(this._targetSelector);
          if (r) {
            this._dotnetHelper.invokeMethodAsync('OnTargetMoved', r);
          }
        }
      };
      window.addEventListener('resize', this._handler);
      window.addEventListener('scroll', this._handler, true);
    }
  },

  stopTracking() {
    this._targetSelector = null;
    this._dotnetHelper = null;
    if (this._handler) {
      window.removeEventListener('resize', this._handler);
      window.removeEventListener('scroll', this._handler, true);
      this._handler = null;
    }
  }
};

const tentoAudio = {
  init(audioEl, dotnetHelper) {
    if (!audioEl || !dotnetHelper) return;

    const onTimeUpdate = () => {
      dotnetHelper.invokeMethodAsync('OnJsTimeUpdate', audioEl.currentTime);
    };

    const onLoadedMetadata = () => {
      if (Number.isFinite(audioEl.duration) && audioEl.duration > 0) {
        dotnetHelper.invokeMethodAsync('OnJsLoadedMetadata', audioEl.duration);
      }
    };

    const onEnded = () => {
      dotnetHelper.invokeMethodAsync('OnJsEnded');
    };

    const onPlay = () => {
      dotnetHelper.invokeMethodAsync('OnJsPlayStateChanged', true);
    };

    const onPause = () => {
      dotnetHelper.invokeMethodAsync('OnJsPlayStateChanged', false);
    };

    audioEl.addEventListener('timeupdate', onTimeUpdate);
    audioEl.addEventListener('loadedmetadata', onLoadedMetadata);
    audioEl.addEventListener('ended', onEnded);
    audioEl.addEventListener('play', onPlay);
    audioEl.addEventListener('pause', onPause);

    audioEl._tentoAudioCleanup = () => {
      audioEl.removeEventListener('timeupdate', onTimeUpdate);
      audioEl.removeEventListener('loadedmetadata', onLoadedMetadata);
      audioEl.removeEventListener('ended', onEnded);
      audioEl.removeEventListener('play', onPlay);
      audioEl.removeEventListener('pause', onPause);
    };

    // If metadata already loaded
    if (Number.isFinite(audioEl.duration) && audioEl.duration > 0) {
      dotnetHelper.invokeMethodAsync('OnJsLoadedMetadata', audioEl.duration);
    }
  },

  play(audioEl) {
    if (!audioEl) return Promise.resolve();
    try {
      const p = audioEl.play();
      if (p && typeof p.catch === 'function') {
        return p.catch(err => {
          console.warn('TentoUI Audio play warning:', err);
        });
      }
    } catch (e) {
      console.warn('TentoUI Audio play error:', e);
    }
    return Promise.resolve();
  },

  pause(audioEl) {
    if (!audioEl) return;
    try {
      audioEl.pause();
    } catch (e) {}
  },

  setCurrentTime(audioEl, time) {
    if (!audioEl) return;
    try {
      if (Number.isFinite(time)) {
        audioEl.currentTime = time;
      }
    } catch (e) {}
  },

  setVolume(audioEl, volume) {
    if (!audioEl) return;
    try {
      audioEl.volume = Math.max(0, Math.min(1, volume));
    } catch (e) {}
  },

  setPlaybackRate(audioEl, rate) {
    if (!audioEl) return;
    try {
      audioEl.playbackRate = rate;
    } catch (e) {}
  },

  dispose(audioEl) {
    if (audioEl && audioEl._tentoAudioCleanup) {
      audioEl._tentoAudioCleanup();
      delete audioEl._tentoAudioCleanup;
    }
  }
};

const tentoCommandPalette = {
  _listenerAttached: false,
  _activeCallbacks: new Set(),

  initShortcut() {
    if (this._listenerAttached) return;
    this._listenerAttached = true;

    window.addEventListener('keydown', (e) => {
      if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
        e.preventDefault();
        e.stopPropagation();
        for (const cb of this._activeCallbacks) {
          try {
            cb();
          } catch (err) {
            console.error(err);
          }
        }
      }
    }, { capture: true });
  },

  registerCallback(dotNetHelper, methodName) {
    this.initShortcut();
    const cb = () => {
      dotNetHelper.invokeMethodAsync(methodName);
    };
    this._activeCallbacks.add(cb);
    return {
      dispose: () => this._activeCallbacks.delete(cb)
    };
  },

  focusInput(selector) {
    setTimeout(() => {
      const input = document.querySelector(selector || '.tento-command-palette-input');
      if (input) {
        input.focus();
        input.select();
      }
    }, 20);
  }
};

const tentoToc = {
  _observer: null,
  _activeId: null,
  _dotNetHelper: null,

  scan(dotNetHelper) {
    this._dotNetHelper = dotNetHelper;
    if (this._observer) {
      this._observer.disconnect();
      this._observer = null;
    }

    const main = document.querySelector('main');
    if (!main) return [];

    const items = [];
    const slugCounts = {};

    function makeSlug(text) {
      const base = (text || 'section')
        .toLowerCase()
        .replace(/[^\w\s-]/g, '')
        .trim()
        .replace(/\s+/g, '-');
      if (!slugCounts[base]) {
        slugCounts[base] = 1;
        return base;
      }
      return `${base}-${slugCounts[base]++}`;
    }

    // 1. Interactive PropsPlayground
    const playgrounds = main.querySelectorAll('.tento-props-playground');
    playgrounds.forEach((pg) => {
      const h3 = pg.querySelector('h3');
      const title = h3 ? h3.textContent.trim() : 'Interactive Playground';
      const id = pg.id || makeSlug(title);
      pg.id = id;
      pg.classList.add('scroll-mt-24');
      items.push({ id, title, category: 'playground' });
    });

    // 2. Demo Cards (Examples)
    const demoCards = main.querySelectorAll('.tento-demo-card');
    demoCards.forEach((dc) => {
      const h3 = dc.querySelector('h3');
      if (!h3) return;
      const title = h3.textContent.trim();
      const id = dc.id || makeSlug(title);
      dc.id = id;
      dc.classList.add('scroll-mt-24');
      items.push({ id, title, category: 'example' });
    });

    // 3. API Reference Tables
    const apiTables = main.querySelectorAll('.tento-api-table, .mt-8:has(table)');
    apiTables.forEach((at) => {
      const h3 = at.querySelector('h3');
      if (!h3) return;
      const title = h3.textContent.trim();
      const id = at.id || makeSlug(title);
      at.id = id;
      at.classList.add('scroll-mt-24');
      items.push({ id, title, category: 'api' });
    });

    // 4. Fallback: if no playgrounds, demo cards or API tables, scan h2 and h3
    if (items.length === 0) {
      const headings = main.querySelectorAll('h2, h3');
      headings.forEach((h) => {
        const title = h.textContent.trim();
        if (!title) return;
        const id = h.id || makeSlug(title);
        h.id = id;
        h.classList.add('scroll-mt-24');
        items.push({ id, title, category: h.tagName.toLowerCase() === 'h2' ? 'heading-2' : 'heading-3' });
      });
    }

    // Setup IntersectionObserver for smooth scroll spy
    const observedElements = items.map(item => document.getElementById(item.id)).filter(Boolean);
    if (observedElements.length > 0 && typeof IntersectionObserver !== 'undefined') {
      this._observer = new IntersectionObserver((entries) => {
        const visibleEntries = entries.filter(e => e.isIntersecting);
        if (visibleEntries.length > 0) {
          visibleEntries.sort((a, b) => Math.abs(a.boundingClientRect.top - 100) - Math.abs(b.boundingClientRect.top - 100));
          const topEntry = visibleEntries[0];
          tentoToc.setActiveItem(topEntry.target.id);
        }
      }, {
        rootMargin: '-80px 0px -65% 0px',
        threshold: [0, 0.25, 0.5]
      });

      observedElements.forEach(el => this._observer.observe(el));
    }

    return items;
  },

  setActiveItem(id) {
    if (this._activeId === id) return;
    this._activeId = id;

    const links = document.querySelectorAll('.tento-toc-item');
    links.forEach(link => {
      const targetId = link.getAttribute('data-target-id');
      const isTarget = targetId === id;
      link.classList.toggle('text-primary-600', isTarget);
      link.classList.toggle('dark:text-primary-400', isTarget);
      link.classList.toggle('font-semibold', isTarget);
      link.classList.toggle('border-primary-500', isTarget);
      link.classList.toggle('text-slate-500', !isTarget);
      link.classList.toggle('dark:text-slate-400', !isTarget);
      link.classList.toggle('border-transparent', !isTarget);
    });

    if (this._dotNetHelper) {
      try {
        this._dotNetHelper.invokeMethodAsync('OnActiveSectionChanged', id);
      } catch {}
    }
  },

  scrollTo(id) {
    const el = document.getElementById(id);
    if (el) {
      el.scrollIntoView({ behavior: 'smooth', block: 'start' });
      this.setActiveItem(id);
    }
  },

  cleanup() {
    if (this._observer) {
      this._observer.disconnect();
      this._observer = null;
    }
    this._dotNetHelper = null;
  }
};

// Expose to window for standard script tag usage
if (typeof window !== 'undefined') {
  window.tentoModal = tentoModal;
  window.tentoClipboard = tentoClipboard;
  window.tentoTour = tentoTour;
  window.tentoAudio = tentoAudio;
  window.tentoCommandPalette = tentoCommandPalette;
  window.tentoToc = tentoToc;

  // Auto-register capture-phase prevention for Ctrl+K
  tentoCommandPalette.initShortcut();
}

// Export for module bundlers / CommonJS if supported
if (typeof module !== 'undefined' && module.exports) {
  module.exports = { tentoModal, tentoClipboard, tentoTour, tentoAudio, tentoCommandPalette, tentoToc };
}
