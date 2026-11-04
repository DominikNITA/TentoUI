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

// Expose to window for standard script tag usage
if (typeof window !== 'undefined') {
  window.tentoModal = tentoModal;
  window.tentoClipboard = tentoClipboard;
  window.tentoTour = tentoTour;
}

// Export for module bundlers / CommonJS if supported
if (typeof module !== 'undefined' && module.exports) {
  module.exports = { tentoModal, tentoClipboard, tentoTour };
}
