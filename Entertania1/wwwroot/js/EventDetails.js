// ===============================================
// EVENT DETAILS - PART 1: MEDIA GALLERY & CONTROLS
// ===============================================

// State Management
let currentMediaIndex = 0;
let autoplayInterval = null;
let isAutoplayActive = true;
let lastScrollY = 0;
const AUTOPLAY_DURATION = 5000; // 5 seconds

// ===============================================
// INITIALIZATION
// ===============================================

document.addEventListener('DOMContentLoaded', function () {
    initializeMediaGallery();
    initializeStickyBookBar();
    initializeVenueMap();
    loadSubEventDetails();
    initializeFadeAnimations();

    console.log('✅ Event Details Initialized');
});

// ===============================================
// MEDIA GALLERY
// ===============================================

function initializeMediaGallery() {
    if (!mediaItems || mediaItems.length === 0) return;

    // Find primary media or use first
    const primaryIndex = mediaItems.findIndex(item => item.isPrimary);
    currentMediaIndex = primaryIndex >= 0 ? primaryIndex : 0;

    updateMedia();

    if (mediaItems.length > 1 && isAutoplayActive) {
        startAutoplay();
    }

    // Pause autoplay on hover
    const heroGallery = document.getElementById('heroGallery');
    if (heroGallery) {
        heroGallery.addEventListener('mouseenter', pauseAutoplay);
        heroGallery.addEventListener('mouseleave', () => {
            if (isAutoplayActive && mediaItems.length > 1) {
                startAutoplay();
            }
        });
    }

    // Keyboard navigation
    document.addEventListener('keydown', handleKeyboard);

    // Touch gestures
    if (heroGallery) {
        heroGallery.addEventListener('touchstart', handleTouchStart, false);
        heroGallery.addEventListener('touchend', handleTouchEnd, false);
    }

    // Page visibility
    document.addEventListener('visibilitychange', handleVisibilityChange);
}

function updateMedia() {
    if (!mediaItems || mediaItems.length === 0) return;

    // Save current scroll position
    const scrollPos = window.scrollY;

    const viewport = document.getElementById('heroMediaViewport');
    const modalContent = document.getElementById('modalContent');
    const currentMedia = mediaItems[currentMediaIndex];

    // Clear existing
    if (viewport) viewport.innerHTML = '';
    if (modalContent) modalContent.innerHTML = '';

    // Create media elements
    const viewportElement = createMediaElement(currentMedia);
    const modalElement = createMediaElement(currentMedia);

    // Add to containers
    if (viewport) viewport.appendChild(viewportElement);
    if (modalContent) modalContent.appendChild(modalElement);

    // Update counter
    const currentNum = document.getElementById('currentNum');
    const totalNum = document.getElementById('totalNum');
    if (currentNum) currentNum.textContent = currentMediaIndex + 1;
    if (totalNum) totalNum.textContent = mediaItems.length;

    // Update thumbnails
    updateThumbnails();

    // Reset progress
    resetProgress();

    // Restore scroll position
    window.scrollTo(0, scrollPos);
}

function createMediaElement(media) {
    const wrapper = document.createElement('div');
    wrapper.className = 'media-item active';

    if (media.type === 'photo') {
        const img = document.createElement('img');
        img.src = media.src;
        img.alt = 'Event Image';
        img.loading = 'lazy';
        wrapper.appendChild(img);
    } else if (media.type === 'video') {
        const video = document.createElement('video');
        video.controls = true;
        video.style.width = '100%';
        video.style.height = '100%';
        video.style.objectFit = 'cover';

        const source = document.createElement('source');
        source.src = media.src;
        source.type = media.contentType;

        video.appendChild(source);
        wrapper.appendChild(video);

        // Pause autoplay when video plays
        video.addEventListener('play', pauseAutoplay);
        video.addEventListener('pause', () => {
            if (isAutoplayActive) startAutoplay();
        });
    }

    return wrapper;
}

function navigateMedia(direction) {
    if (!mediaItems || mediaItems.length === 0) return;

    // Save scroll position before navigation
    const scrollPos = window.scrollY;

    currentMediaIndex += direction;

    if (currentMediaIndex < 0) {
        currentMediaIndex = mediaItems.length - 1;
    } else if (currentMediaIndex >= mediaItems.length) {
        currentMediaIndex = 0;
    }

    updateMedia();

    // Restore scroll position after update
    setTimeout(() => {
        window.scrollTo(0, scrollPos);
    }, 0);
}

function selectMedia(index) {
    if (!mediaItems || mediaItems.length === 0) return;

    // Save scroll position before selection
    const scrollPos = window.scrollY;

    currentMediaIndex = index;
    updateMedia();

    // Restore scroll position after update
    setTimeout(() => {
        window.scrollTo(0, scrollPos);
    }, 0);
}

function updateThumbnails() {
    const thumbnails = document.querySelectorAll('.thumb-item');
    thumbnails.forEach((thumb, index) => {
        thumb.classList.toggle('active', index === currentMediaIndex);
    });

    // Scroll active thumbnail into view
    const activeThumb = document.querySelector('.thumb-item.active');
    if (activeThumb) {
        // Save scroll position before thumbnail scroll
        const pageScrollPos = window.scrollY;

        activeThumb.scrollIntoView({
            behavior: 'smooth',
            block: 'nearest'
        });

        // Restore page scroll position after thumbnail container scrolls
        setTimeout(() => {
            window.scrollTo(0, pageScrollPos);
        }, 50);
    }
}

// ===============================================
// AUTOPLAY CONTROLS
// ===============================================

function startAutoplay() {
    if (autoplayInterval) clearInterval(autoplayInterval);

    autoplayInterval = setInterval(() => {
        navigateMedia(1);
    }, AUTOPLAY_DURATION);

    animateProgress();
}

function pauseAutoplay() {
    if (autoplayInterval) {
        clearInterval(autoplayInterval);
        autoplayInterval = null;
    }
    resetProgress();
}

function toggleAutoplay() {
    isAutoplayActive = !isAutoplayActive;
    const btn = document.getElementById('playPauseBtn');

    if (isAutoplayActive) {
        if (btn) btn.innerHTML = '<i class="fas fa-pause"></i>';
        startAutoplay();
    } else {
        if (btn) btn.innerHTML = '<i class="fas fa-play"></i>';
        pauseAutoplay();
    }
}

function animateProgress() {
    const progressFill = document.getElementById('progressFill');
    if (!progressFill) return;

    progressFill.style.transition = 'none';
    progressFill.style.width = '0%';

    setTimeout(() => {
        progressFill.style.transition = `width ${AUTOPLAY_DURATION}ms linear`;
        progressFill.style.width = '100%';
    }, 50);
}

function resetProgress() {
    const progressFill = document.getElementById('progressFill');
    if (progressFill) {
        progressFill.style.transition = 'none';
        progressFill.style.width = '0%';
    }
}

// ===============================================
// FULLSCREEN MODAL
// ===============================================

function toggleFullscreen() {
    const modal = document.getElementById('fullscreenModal');
    if (modal.classList.contains('active')) {
        closeFullscreen();
    } else {
        openFullscreen();
    }
}

function openFullscreen() {
    const modal = document.getElementById('fullscreenModal');
    modal.classList.add('active');
    document.body.style.overflow = 'hidden';
    pauseAutoplay();
    updateMedia();
}

function closeFullscreen() {
    const modal = document.getElementById('fullscreenModal');
    modal.classList.remove('active');
    document.body.style.overflow = '';

    if (isAutoplayActive && mediaItems.length > 1) {
        startAutoplay();
    }
}

// ===============================================
// EVENT HANDLERS
// ===============================================

function handleKeyboard(e) {
    if (!mediaItems || mediaItems.length === 0) return;

    const modal = document.getElementById('fullscreenModal');
    const isModalOpen = modal && modal.classList.contains('active');

    if (e.key === 'ArrowLeft') {
        navigateMedia(-1);
    } else if (e.key === 'ArrowRight') {
        navigateMedia(1);
    } else if (e.key === 'Escape' && isModalOpen) {
        closeFullscreen();
    } else if (e.key === ' ' && !isModalOpen) {
        e.preventDefault();
        toggleAutoplay();
    }
}

let touchStartX = 0;
let touchEndX = 0;

function handleTouchStart(e) {
    touchStartX = e.changedTouches[0].screenX;
}

function handleTouchEnd(e) {
    touchEndX = e.changedTouches[0].screenX;
    handleSwipe();
}

function handleSwipe() {
    const swipeThreshold = 50;
    const diff = touchStartX - touchEndX;

    if (Math.abs(diff) > swipeThreshold) {
        if (diff > 0) {
            navigateMedia(1); // Swipe left
        } else {
            navigateMedia(-1); // Swipe right
        }
    }
}

function handleVisibilityChange() {
    if (document.hidden) {
        pauseAutoplay();
    } else {
        if (isAutoplayActive && mediaItems && mediaItems.length > 1) {
            startAutoplay();
        }
    }
}

// ===============================================
// STICKY BOOK BAR
// ===============================================

function initializeStickyBookBar() {
    const stickyBar = document.getElementById('stickyBookBar');
    if (!stickyBar) return;

    const heroGallery = document.getElementById('heroGallery');
    if (!heroGallery) return;

    const observer = new IntersectionObserver(
        (entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    stickyBar.classList.remove('visible');
                } else {
                    stickyBar.classList.add('visible');
                }
            });
        },
        { threshold: 0.1, rootMargin: '0px 0px -100px 0px' }
    );

    observer.observe(heroGallery);
}

// ===============================================
// EVENT DETAILS - PART 2: MAP, SHARE & UTILITIES
// ===============================================

// ===============================================
// VENUE MAP
// ===============================================

function initializeVenueMap() {
    if (!venueData || typeof venueData.lat !== 'number' || typeof venueData.lng !== 'number') return;

    const mapElement = document.getElementById('venueMap');
    if (!mapElement) return;

    // Initialize Leaflet map
    const map = L.map('venueMap').setView([venueData.lat, venueData.lng], 15);

    // Add tile layer
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '© OpenStreetMap contributors'
    }).addTo(map);

    // Add marker
    const marker = L.marker([venueData.lat, venueData.lng]).addTo(map);

    // Popup content
    const popupContent = `
        <div style="padding: 8px; max-width: 220px; font-family: inherit;">
            <strong style="color: var(--color-accent-start); font-size: 1rem;">${venueData.name || ''}</strong><br/>
            <small style="color: var(--color-text-secondary); margin-top: 4px; display: block;">
                <i class="fas fa-map-marker-alt"></i> ${venueData.location || ''}
            </small>
        </div>
    `;

    marker.bindPopup(popupContent).openPopup();

    // Directions button
    const directionsBtn = document.getElementById('directionsBtn');
    if (directionsBtn) {
        directionsBtn.addEventListener('click', getDirections);
    }

    // Handle window resize
    let resizeTimeout;
    window.addEventListener('resize', function () {
        clearTimeout(resizeTimeout);
        resizeTimeout = setTimeout(() => {
            map.invalidateSize();
        }, 250);
    });
}

function getDirections() {
    if (!navigator.geolocation) {
        showNotification('Geolocation is not supported by your browser.', 'error');
        return;
    }

    navigator.geolocation.getCurrentPosition(
        position => {
            const userLat = position.coords.latitude.toFixed(6);
            const userLng = position.coords.longitude.toFixed(6);
            const url = `https://www.openstreetmap.org/directions?engine=fossgis_osrm_car&route=${userLat},${userLng};${venueData.lat},${venueData.lng}`;
            window.open(url, '_blank', 'noopener');
        },
        error => {
            console.error('Geolocation error:', error);
            showNotification('Could not get your location. Please enable GPS and try again.', 'error');
        }
    );
}

// ===============================================
// SUB-EVENT DETAILS
// ===============================================

function loadSubEventDetails() {
    if (!eventData || !eventData.category || !eventData.eventId) return;

    const container = document.getElementById('subEventContainer');
    if (!container) return;

    fetch(`/Events/LoadCategoryDetails?category=${encodeURIComponent(eventData.category)}&id=${eventData.eventId}`)
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.text();
        })
        .then(html => {
            if (html && html.trim()) {
                container.innerHTML = html;
                container.style.display = 'block';
            }
        })
        .catch(error => {
            console.error('Error loading sub-event details:', error);
        });
}

// ===============================================
// SHARE FUNCTIONALITY
// ===============================================

function shareEvent(platform) {
    const url = encodeURIComponent(window.location.href);
    const title = encodeURIComponent(document.querySelector('.event-title')?.textContent || 'Event');
    let shareUrl = '';

    switch (platform) {
        case 'facebook':
            shareUrl = `https://www.facebook.com/sharer/sharer.php?u=${url}`;
            break;
        case 'twitter':
            shareUrl = `https://twitter.com/intent/tweet?url=${url}&text=${title}`;
            break;
        case 'whatsapp':
            shareUrl = `https://wa.me/?text=${title}%20${url}`;
            break;
    }

    if (shareUrl) {
        window.open(shareUrl, '_blank', 'width=600,height=400');
    }
}

function copyLink() {
    const url = window.location.href;

    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(url)
            .then(() => {
                showNotification('Link copied to clipboard!', 'success');
            })
            .catch(err => {
                console.error('Failed to copy:', err);
                fallbackCopy(url);
            });
    } else {
        fallbackCopy(url);
    }
}

function fallbackCopy(url) {
    const tempInput = document.createElement('input');
    tempInput.value = url;
    document.body.appendChild(tempInput);
    tempInput.select();

    try {
        document.execCommand('copy');
        showNotification('Link copied to clipboard!', 'success');
    } catch (err) {
        console.error('Fallback copy failed:', err);
        showNotification('Failed to copy link', 'error');
    }

    document.body.removeChild(tempInput);
}

// ===============================================
// NOTIFICATIONS
// ===============================================

function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `alert alert-${type === 'success' ? 'success' : 'danger'} alert-dismissible fade show`;
    notification.style.cssText = `
        position: fixed;
        top: 6rem;
        right: 1rem;
        z-index: 10000;
        min-width: 300px;
        animation: slideInRight 0.4s ease;
    `;
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    document.body.appendChild(notification);

    setTimeout(() => {
        notification.remove();
    }, 3000);
}

// ===============================================
// FADE-IN ANIMATIONS
// ===============================================

function initializeFadeAnimations() {
    const observer = new IntersectionObserver(
        (entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                }
            });
        },
        { threshold: 0.1 }
    );

    document.querySelectorAll('.fade-in').forEach(el => {
        observer.observe(el);
    });
}

// ===============================================
// LAZY LOAD IMAGES
// ===============================================

function lazyLoadImages() {
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    if (img.dataset.src) {
                        img.src = img.dataset.src;
                        img.removeAttribute('data-src');
                    }
                    imageObserver.unobserve(img);
                }
            });
        });

        document.querySelectorAll('img[data-src]').forEach(img => {
            imageObserver.observe(img);
        });
    }
}

// ===============================================
// CLEANUP
// ===============================================

window.addEventListener('beforeunload', function () {
    if (autoplayInterval) {
        clearInterval(autoplayInterval);
    }
});

// ===============================================
// END OF SCRIPT
// ===============================================