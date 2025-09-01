// Site-wide JavaScript functionality for Kometa GUI v3

// Initialize application when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initializeTabs();
    initializeTooltips();
    initializeModals();
    initializeKeyboardShortcuts();
});

// Tab functionality
function initializeTabs() {
    document.querySelectorAll('.tab-button').forEach(button => {
        button.addEventListener('click', function() {
            const targetTab = this.getAttribute('data-tab');
            const tabGroup = this.closest('.tabs');
            
            // Remove active class from all tabs in this group
            tabGroup.querySelectorAll('.tab-button').forEach(btn => btn.classList.remove('active'));
            tabGroup.querySelectorAll('.tab-content').forEach(content => content.classList.remove('active'));
            
            // Add active class to clicked tab and corresponding content
            this.classList.add('active');
            const targetContent = tabGroup.querySelector(`[data-tab-content="${targetTab}"]`);
            if (targetContent) {
                targetContent.classList.add('active');
            }
        });
    });
}

// Tooltip functionality
function initializeTooltips() {
    document.querySelectorAll('[data-tooltip]').forEach(element => {
        element.addEventListener('mouseenter', showTooltip);
        element.addEventListener('mouseleave', hideTooltip);
        element.addEventListener('focus', showTooltip);
        element.addEventListener('blur', hideTooltip);
    });
}

function showTooltip(e) {
    const tooltipText = e.target.getAttribute('data-tooltip');
    if (!tooltipText) return;
    
    const tooltip = document.createElement('div');
    tooltip.className = 'tooltip';
    tooltip.textContent = tooltipText;
    tooltip.style.cssText = `
        position: absolute;
        background: var(--bg-tertiary);
        color: var(--text-primary);
        padding: 8px 12px;
        border-radius: 4px;
        font-size: 0.85rem;
        z-index: 1000;
        border: 1px solid var(--border);
        box-shadow: 0 4px 12px var(--shadow);
        max-width: 250px;
        word-wrap: break-word;
        pointer-events: none;
    `;
    
    document.body.appendChild(tooltip);
    
    const rect = e.target.getBoundingClientRect();
    const tooltipRect = tooltip.getBoundingClientRect();
    
    let top = rect.top - tooltipRect.height - 8;
    let left = rect.left + (rect.width / 2) - (tooltipRect.width / 2);
    
    // Adjust if tooltip would be off-screen
    if (top < 8) {
        top = rect.bottom + 8;
    }
    if (left < 8) {
        left = 8;
    } else if (left + tooltipRect.width > window.innerWidth - 8) {
        left = window.innerWidth - tooltipRect.width - 8;
    }
    
    tooltip.style.top = top + window.scrollY + 'px';
    tooltip.style.left = left + 'px';
    
    e.target._tooltip = tooltip;
}

function hideTooltip(e) {
    if (e.target._tooltip) {
        e.target._tooltip.remove();
        e.target._tooltip = null;
    }
}

// Modal functionality
function initializeModals() {
    document.querySelectorAll('[data-modal]').forEach(trigger => {
        trigger.addEventListener('click', function(e) {
            e.preventDefault();
            const modalId = this.getAttribute('data-modal');
            const modal = document.getElementById(modalId);
            if (modal) {
                showModal(modal);
            }
        });
    });
    
    document.querySelectorAll('.modal-close, [data-modal-close]').forEach(closeBtn => {
        closeBtn.addEventListener('click', function(e) {
            e.preventDefault();
            const modal = this.closest('.modal');
            if (modal) {
                hideModal(modal);
            }
        });
    });
}

function showModal(modal) {
    modal.style.display = 'block';
    modal.classList.add('active');
    document.body.style.overflow = 'hidden';
    
    // Focus first focusable element in modal
    const focusable = modal.querySelector('input, button, select, textarea, [tabindex]:not([tabindex="-1"])');
    if (focusable) {
        setTimeout(() => focusable.focus(), 100);
    }
}

function hideModal(modal) {
    modal.style.display = 'none';
    modal.classList.remove('active');
    document.body.style.overflow = '';
}

// Keyboard shortcuts
function initializeKeyboardShortcuts() {
    document.addEventListener('keydown', function(e) {
        // Escape key closes modals
        if (e.key === 'Escape') {
            const activeModal = document.querySelector('.modal.active');
            if (activeModal) {
                hideModal(activeModal);
                return;
            }
        }
        
        // Ctrl/Cmd + S to save (prevent default and trigger save if available)
        if ((e.ctrlKey || e.metaKey) && e.key === 's') {
            e.preventDefault();
            const saveBtn = document.querySelector('[data-save], .btn-save, #save-btn');
            if (saveBtn && !saveBtn.disabled) {
                saveBtn.click();
            }
        }
        
        // Alt + N for Next button
        if (e.altKey && e.key === 'n') {
            e.preventDefault();
            const nextBtn = document.querySelector('#next-btn, .btn-next');
            if (nextBtn && !nextBtn.disabled) {
                nextBtn.click();
            }
        }
        
        // Alt + B for Back button
        if (e.altKey && e.key === 'b') {
            e.preventDefault();
            const backBtn = document.querySelector('#back-btn, .btn-back');
            if (backBtn && !backBtn.disabled) {
                backBtn.click();
            }
        }
    });
}

// Utility functions
window.KometaUtils = {
    // Show loading state for a button
    showButtonLoading: function(button, loadingText = 'Loading...') {
        if (button._originalText) return; // Already loading
        
        button._originalText = button.textContent;
        button._originalDisabled = button.disabled;
        
        button.disabled = true;
        button.innerHTML = `<span class="spinner"></span> ${loadingText}`;
    },
    
    // Hide loading state for a button
    hideButtonLoading: function(button) {
        if (!button._originalText) return; // Not loading
        
        button.disabled = button._originalDisabled;
        button.textContent = button._originalText;
        
        delete button._originalText;
        delete button._originalDisabled;
    },
    
    // Format file size
    formatFileSize: function(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    },
    
    // Format date
    formatDate: function(date, format = 'short') {
        const d = new Date(date);
        if (format === 'short') {
            return d.toLocaleDateString();
        } else if (format === 'long') {
            return d.toLocaleDateString() + ' ' + d.toLocaleTimeString();
        }
        return d.toString();
    },
    
    // Debounce function
    debounce: function(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    },
    
    // Throttle function
    throttle: function(func, limit) {
        let inThrottle;
        return function() {
            const args = arguments;
            const context = this;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    },
    
    // Copy text to clipboard
    copyToClipboard: function(text) {
        if (navigator.clipboard) {
            return navigator.clipboard.writeText(text);
        } else {
            // Fallback for older browsers
            const textArea = document.createElement('textarea');
            textArea.value = text;
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            try {
                document.execCommand('copy');
                return Promise.resolve();
            } catch (err) {
                return Promise.reject(err);
            } finally {
                document.body.removeChild(textArea);
            }
        }
    },
    
    // Show notification
    showNotification: function(message, type = 'info', duration = 3000) {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 1000;
            background: var(--bg-secondary);
            color: var(--text-primary);
            padding: 16px 20px;
            border-radius: 6px;
            border-left: 4px solid var(--accent-primary);
            box-shadow: 0 4px 12px var(--shadow);
            max-width: 300px;
            opacity: 0;
            transform: translateX(100%);
            transition: all 0.3s ease;
        `;
        
        // Type-specific styling
        if (type === 'success') {
            notification.style.borderLeftColor = 'var(--success)';
        } else if (type === 'warning') {
            notification.style.borderLeftColor = 'var(--warning)';
        } else if (type === 'error' || type === 'danger') {
            notification.style.borderLeftColor = 'var(--danger)';
            duration = 5000; // Error messages show longer
        }
        
        document.body.appendChild(notification);
        
        // Animate in
        setTimeout(() => {
            notification.style.opacity = '1';
            notification.style.transform = 'translateX(0)';
        }, 10);
        
        // Auto remove
        setTimeout(() => {
            notification.style.opacity = '0';
            notification.style.transform = 'translateX(100%)';
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.parentNode.removeChild(notification);
                }
            }, 300);
        }, duration);
        
        return notification;
    }
};

// Global error handler
window.addEventListener('error', function(e) {
    console.error('Global error:', e.error);
    if (e.error && !e.error.toString().includes('Script error')) {
        KometaUtils.showNotification('An unexpected error occurred. Please refresh the page.', 'error');
    }
});

// Handle SignalR connection errors gracefully
window.addEventListener('unhandledrejection', function(e) {
    if (e.reason && e.reason.toString().includes('HubConnection')) {
        console.warn('SignalR connection issue:', e.reason);
        // Don't show user notification for SignalR issues as they're handled in the client
        e.preventDefault();
    }
});