// ============================================
// VENUE INDEX - JavaScript
// ============================================

// Carousel Management
const venueCarousels = {};

document.addEventListener('DOMContentLoaded', function () {
    initializeSelect2();
    initializeCarousels();
    initializeViewToggle();
    initializeGovernorateFilter();
});

// ============================================
// SELECT2 INITIALIZATION
// ============================================
function initializeSelect2() {
    $('.select2-governorate-filter').select2({
        placeholder: 'All Governorates',
        allowClear: true,
        width: '100%'
    });
}

// ============================================
// CAROUSEL INITIALIZATION
// ============================================
function initializeCarousels() {
    document.querySelectorAll('.venue-photo-carousel').forEach(carousel => {
        const venueId = carousel.dataset.venueId;
        const mediaItems = carousel.querySelectorAll('.venue-media');

        if (mediaItems.length > 0) {
            mediaItems[0].classList.add('active');

            venueCarousels[venueId] = {
                currentIndex: 0,
                mediaItems: mediaItems,
                indicators: carousel.querySelectorAll('.indicator'),
                interval: null
            };

            startVenueAutoRotate(venueId);

            carousel.addEventListener('mouseenter', () => stopVenueAutoRotate(venueId));
            carousel.addEventListener('mouseleave', () => startVenueAutoRotate(venueId));

            // Video click handling
            mediaItems.forEach((item) => {
                if (item.dataset.type === 'video') {
                    const video = item.querySelector('video');
                    const playIcon = item.querySelector('.video-play-icon');

                    item.addEventListener('click', (e) => {
                        e.stopPropagation();
                        if (video.paused) {
                            video.play();
                            playIcon.style.display = 'none';
                            stopVenueAutoRotate(venueId);
                        } else {
                            video.pause();
                            playIcon.style.display = 'flex';
                        }
                    });

                    video.addEventListener('ended', () => {
                        playIcon.style.display = 'flex';
                        startVenueAutoRotate(venueId);
                    });
                }
            });
        }
    });
}

function startVenueAutoRotate(venueId) {
    const carousel = venueCarousels[venueId];
    if (!carousel || carousel.mediaItems.length <= 1) return;

    carousel.interval = setInterval(() => {
        const currentItem = carousel.mediaItems[carousel.currentIndex];
        if (currentItem.dataset.type === 'video') {
            const video = currentItem.querySelector('video');
            if (video && !video.paused) return;
        }
        nextVenueMedia(venueId);
    }, 4000); // 4 seconds
}

function stopVenueAutoRotate(venueId) {
    const carousel = venueCarousels[venueId];
    if (carousel && carousel.interval) {
        clearInterval(carousel.interval);
        carousel.interval = null;
    }
}

function nextVenueMedia(venueId) {
    const carousel = venueCarousels[venueId];
    if (!carousel) return;

    const currentItem = carousel.mediaItems[carousel.currentIndex];
    if (currentItem.dataset.type === 'video') {
        const video = currentItem.querySelector('video');
        if (video && !video.paused) {
            video.pause();
            video.currentTime = 0;
        }
    }

    carousel.currentIndex = (carousel.currentIndex + 1) % carousel.mediaItems.length;
    updateVenueCarousel(venueId);
}

function prevVenueMedia(venueId) {
    const carousel = venueCarousels[venueId];
    if (!carousel) return;

    const currentItem = carousel.mediaItems[carousel.currentIndex];
    if (currentItem.dataset.type === 'video') {
        const video = currentItem.querySelector('video');
        if (video && !video.paused) {
            video.pause();
            video.currentTime = 0;
        }
    }

    carousel.currentIndex = (carousel.currentIndex - 1 + carousel.mediaItems.length) % carousel.mediaItems.length;
    updateVenueCarousel(venueId);
}

function goToVenueMedia(venueId, index) {
    const carousel = venueCarousels[venueId];
    if (!carousel) return;

    const currentItem = carousel.mediaItems[carousel.currentIndex];
    if (currentItem.dataset.type === 'video') {
        const video = currentItem.querySelector('video');
        if (video && !video.paused) {
            video.pause();
            video.currentTime = 0;
        }
    }

    carousel.currentIndex = index;
    updateVenueCarousel(venueId);

    stopVenueAutoRotate(venueId);
    startVenueAutoRotate(venueId);
}

function updateVenueCarousel(venueId) {
    const carousel = venueCarousels[venueId];
    if (!carousel) return;

    carousel.mediaItems.forEach((item, index) => {
        item.classList.toggle('active', index === carousel.currentIndex);

        if (item.dataset.type === 'video' && index !== carousel.currentIndex) {
            const playIcon = item.querySelector('.video-play-icon');
            if (playIcon) playIcon.style.display = 'flex';
        }
    });

    carousel.indicators.forEach((indicator, index) => {
        indicator.classList.toggle('active', index === carousel.currentIndex);
    });
}

// ============================================
// VIEW TOGGLE (GRID / LIST / MAP)
// ============================================
function initializeViewToggle() {
    const viewButtons = document.querySelectorAll('.view-btn');
    const gridView = document.getElementById('gridView');
    const listView = document.getElementById('listView');
    const mapView = document.getElementById('mapView');

    viewButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            const view = this.dataset.view;

            // Update button states
            viewButtons.forEach(b => b.classList.remove('active'));
            this.classList.add('active');

            // Toggle views
            if (view === 'grid') {
                gridView.style.display = 'grid';
                listView.classList.remove('active');
                mapView.classList.remove('active');
            } else if (view === 'list') {
                gridView.style.display = 'none';
                listView.classList.add('active');
                mapView.classList.remove('active');
            } else if (view === 'map') {
                gridView.style.display = 'none';
                listView.classList.remove('active');
                mapView.classList.add('active');
                initializeMap();
            }
        });
    });
}

// ============================================
// MAP INITIALIZATION (OpenStreetMap + Leaflet)
// ============================================
let venueMap = null;

function initializeMap() {
    if (venueMap) return; // Already initialized

    const mapContainer = document.getElementById('venueMap');
    if (!mapContainer || !window.venuesData) return;

    // Filter venues with coordinates
    const venuesWithCoords = window.venuesData.filter(v => v.lat && v.lng);
    if (venuesWithCoords.length === 0) {
        mapContainer.innerHTML = '<div style="display:flex;align-items:center;justify-content:center;height:100%;color:var(--muted-text);"><i class="fas fa-map-marked-alt fa-3x"></i><p style="margin-left:1rem;">No venues with GPS coordinates</p></div>';
        return;
    }

    // Default center (Cairo, Egypt)
    const defaultCenter = [30.0444, 31.2357];
    const center = venuesWithCoords.length > 0
        ? [venuesWithCoords[0].lat, venuesWithCoords[0].lng]
        : defaultCenter;

    // Initialize map
    venueMap = L.map('venueMap').setView(center, 7);

    // Add OpenStreetMap tiles
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        maxZoom: 19
    }).addTo(venueMap);

    // Add markers
    const bounds = [];
    venuesWithCoords.forEach(venue => {
        const marker = L.marker([venue.lat, venue.lng]).addTo(venueMap);

        // Status badge HTML
        let statusBadge = '';
        if (venue.status === 'Approved') {
            statusBadge = '<span style="background:#10b981;color:white;padding:0.25rem 0.75rem;border-radius:50px;font-size:0.8rem;font-weight:600;">✓ Approved</span>';
        } else if (venue.status === 'Pending') {
            statusBadge = '<span style="background:#f59e0b;color:#000;padding:0.25rem 0.75rem;border-radius:50px;font-size:0.8rem;font-weight:600;">⏳ Pending</span>';
        } else {
            statusBadge = '<span style="background:#ef4444;color:white;padding:0.25rem 0.75rem;border-radius:50px;font-size:0.8rem;font-weight:600;">✕ Rejected</span>';
        }

        // Popup content
        const popupContent = `
            <div style="min-width:200px;">
                ${statusBadge}
                <h6 class="map-popup-title" style="margin-top:0.75rem;">${venue.name}</h6>
                <div class="map-popup-meta">
                    <div style="margin-bottom:0.5rem;">
                        <i class="fas fa-map-marked-alt" style="color:var(--gold);margin-right:0.5rem;"></i>
                        ${venue.governorate}
                    </div>
                    <div style="margin-bottom:0.5rem;">
                        <i class="fas fa-map-marker-alt" style="color:var(--gold);margin-right:0.5rem;"></i>
                        ${venue.location}
                    </div>
                    <div>
                        <i class="fas fa-users" style="color:var(--gold);margin-right:0.5rem;"></i>
                        Capacity: ${venue.capacity.toLocaleString()}
                    </div>
                </div>
                <a href="/Venue/Details/${venue.id}" class="map-popup-btn">
                    <i class="fas fa-info-circle"></i> View Details
                </a>
            </div>
        `;

        marker.bindPopup(popupContent);
        bounds.push([venue.lat, venue.lng]);
    });

    // Fit map to show all markers
    if (bounds.length > 1) {
        venueMap.fitBounds(bounds, { padding: [50, 50] });
    }
}

// ============================================
// GOVERNORATE FILTER
// ============================================
function initializeGovernorateFilter() {
    $('.select2-governorate-filter').on('change', function () {
        const form = document.getElementById('filterForm');
        if (form) form.submit();
    });
}

// ============================================
// FILTER MANAGEMENT
// ============================================
function removeFilter(filterName) {
    const form = document.getElementById('filterForm');
    const input = form.querySelector(`[name="${filterName}"]`);
    if (input) {
        input.value = '';
        form.submit();
    }
}

function clearAllFilters() {
    window.location.href = window.location.pathname;
}

// ============================================
// DELETE CONFIRMATION MODAL
// ============================================
function confirmDelete(venueId, venueName) {
    const modal = new bootstrap.Modal(document.getElementById('deleteModal'));
    document.getElementById('venueNameToDelete').textContent = venueName;
    document.getElementById('deleteForm').action = `/Venue/Delete/${venueId}`;
    modal.show();
}