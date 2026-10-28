/**
 * TentoUI Core Interop Layer
 * Provides focus trapping, scroll locking, and accessible boundary management.
 */
export const tentoModal = {
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

export const tentoClipboard = {
  copy(text) {
    if (navigator && navigator.clipboard) {
      return navigator.clipboard.writeText(text);
    }
    return Promise.resolve();
  }
};
