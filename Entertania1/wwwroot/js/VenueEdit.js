// ========================================
// GOVERNORATE DETECTION & VALIDATION
// ========================================

const governorateBounds = {
    1: { name: 'Alexandria', minLat: 30.80, maxLat: 31.35, minLon: 29.70, maxLon: 30.20 },
    2: { name: 'Aswan', minLat: 22.00, maxLat: 24.50, minLon: 31.80, maxLon: 33.50 },
    3: { name: 'Asyut', minLat: 26.90, maxLat: 27.70, minLon: 30.50, maxLon: 31.50 },
    4: { name: 'Beheira', minLat: 30.20, maxLat: 31.50, minLon: 29.90, maxLon: 30.90 },
    5: { name: 'Beni Suef', minLat: 28.70, maxLat: 29.50, minLon: 30.70, maxLon: 31.50 },
    6: { name: 'Cairo', minLat: 29.85, maxLat: 30.20, minLon: 31.10, maxLon: 31.60 },
    7: { name: 'Dakahlia', minLat: 30.80, maxLat: 31.50, minLon: 31.00, maxLon: 32.00 },
    8: { name: 'Damietta', minLat: 31.20, maxLat: 31.60, minLon: 31.60, maxLon: 32.00 },
    9: { name: 'Faiyum', minLat: 28.90, maxLat: 29.80, minLon: 30.20, maxLon: 31.20 },
    10: { name: 'Gharbia', minLat: 30.70, maxLat: 31.20, minLon: 30.80, maxLon: 31.30 },
    11: { name: 'Giza', minLat: 29.40, maxLat: 30.20, minLon: 30.70, maxLon: 31.40 },
    12: { name: 'Ismailia', minLat: 30.30, maxLat: 31.00, minLon: 32.00, maxLon: 32.80 },
    13: { name: 'Kafr El Sheikh', minLat: 31.00, maxLat: 31.50, minLon: 30.50, maxLon: 31.20 },
    14: { name: 'Luxor', minLat: 25.40, maxLat: 25.90, minLon: 32.50, maxLon: 32.90 },
    15: { name: 'Matrouh', minLat: 28.50, maxLat: 31.80, minLon: 24.50, maxLon: 28.50 },
    16: { name: 'Minya', minLat: 27.70, maxLat: 29.00, minLon: 30.40, maxLon: 31.20 },
    17: { name: 'Monufia', minLat: 30.30, maxLat: 30.80, minLon: 30.70, maxLon: 31.30 },
    18: { name: 'New Valley', minLat: 22.00, maxLat: 29.00, minLon: 25.00, maxLon: 31.00 },
    19: { name: 'North Sinai', minLat: 30.00, maxLat: 31.50, minLon: 32.50, maxLon: 34.50 },
    20: { name: 'Port Said', minLat: 31.15, maxLat: 31.35, minLon: 32.20, maxLon: 32.40 },
    21: { name: 'Qalyubia', minLat: 30.05, maxLat: 30.60, minLon: 31.00, maxLon: 31.40 },
    22: { name: 'Qena', minLat: 25.70, maxLat: 26.50, minLon: 32.40, maxLon: 33.20 },
    23: { name: 'Red Sea', minLat: 22.00, maxLat: 29.00, minLon: 32.50, maxLon: 35.50 },
    24: { name: 'Sharqia', minLat: 30.20, maxLat: 31.20, minLon: 31.20, maxLon: 32.20 },
    25: { name: 'Sohag', minLat: 26.20, maxLat: 27.20, minLon: 31.30, maxLon: 32.20 },
    26: { name: 'South Sinai', minLat: 27.50, maxLat: 30.50, minLon: 32.80, maxLon: 34.90 },
    27: { name: 'Suez', minLat: 29.80, maxLat: 30.20, minLon: 32.40, maxLon: 32.70 }
};

const majorCities = [
    { name: 'Mansoura', gov: 7, lat: 31.0379, lon: 31.3815 },
    { name: 'Zagazig', gov: 24, lat: 30.5876, lon: 31.5029 },
    { name: 'Ismailia', gov: 12, lat: 30.5833, lon: 32.2667 },
    { name: 'Cairo', gov: 6, lat: 30.0444, lon: 31.2357 },
    { name: 'Alexandria', gov: 1, lat: 31.2001, lon: 29.9187 },
    { name: 'Tanta', gov: 10, lat: 30.7865, lon: 31.0004 },
    { name: 'Bilbeis', gov: 24, lat: 30.4200, lon: 31.5600 },
    { name: '10th of Ramadan', gov: 24, lat: 30.3000, lon: 31.7500 }
];

function calculateDistance(lat1, lon1, lat2, lon2) {
    const R = 6371;
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
        Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
        Math.sin(dLon / 2) * Math.sin(dLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c;
}

function detectGovernorateFromCoordinates(lat, lon) {
    const candidates = [];
    for (const [id, bounds] of Object.entries(governorateBounds)) {
        if (lat >= bounds.minLat && lat <= bounds.maxLat &&
            lon >= bounds.minLon && lon <= bounds.maxLon) {
            candidates.push(parseInt(id));
        }
    }

    if (candidates.length === 0) {
        let minDist = Infinity;
        let nearest = null;
        for (const city of majorCities) {
            const dist = calculateDistance(lat, lon, city.lat, city.lon);
            if (dist < minDist && dist < 50) {
                minDist = dist;
                nearest = city.gov;
            }
        }
        return nearest;
    }

    if (candidates.length === 1) {
        return candidates[0];
    }

    let minDist = Infinity;
    let nearest = candidates[0];
    for (const city of majorCities) {
        if (candidates.includes(city.gov)) {
            const dist = calculateDistance(lat, lon, city.lat, city.lon);
            if (dist < minDist) {
                minDist = dist;
                nearest = city.gov;
            }
        }
    }
    return nearest;
}

function validateGovernorate(selectedGov, lat, lon) {
    const bounds = governorateBounds[selectedGov];
    if (!bounds) return { valid: false, message: 'Invalid governorate' };

    if (lat >= bounds.minLat && lat <= bounds.maxLat &&
        lon >= bounds.minLon && lon <= bounds.maxLon) {
        return { valid: true, message: '' };
    }

    const tolerance = 0.1;
    if (lat >= bounds.minLat - tolerance && lat <= bounds.maxLat + tolerance &&
        lon >= bounds.minLon - tolerance && lon <= bounds.maxLon + tolerance) {
        const detected = detectGovernorateFromCoordinates(lat, lon);
        if (detected === selectedGov) {
            return { valid: true, message: '' };
        }
    }

    const detected = detectGovernorateFromCoordinates(lat, lon);
    if (detected) {
        const detectedName = governorateBounds[detected].name;
        return {
            valid: false,
            message: `Location appears to be in ${detectedName}, not ${bounds.name}. Please verify or change governorate.`
        };
    }

    return {
        valid: false,
        message: `Location doesn't match ${bounds.name} governorate boundaries.`
    };
}

function validateSelection(govId, lat, lon) {
    const validation = validateGovernorate(govId, lat, lon);
    const validationDiv = $('#governorateValidation');
    const validationMsg = $('#validationMessage');
    const successMsg = $('#autoDetectSuccess');

    if (!validation.valid) {
        validationMsg.text(validation.message);
        validationDiv.show();
        successMsg.hide();
    } else {
        validationDiv.hide();
        successMsg.show();
        setTimeout(() => successMsg.fadeOut(), 3000);
    }
}

// ========================================
// MAP INITIALIZATION - GLOBAL VARS
// ========================================

let map = null;
let marker = null;
const defaultLocation = { lat: 30.0444, lng: 31.2357 };

function initMap() {
    try {
        const mapEl = document.getElementById('map');
        if (!mapEl) {
            console.error('Map element not found');
            return;
        }

        const latInput = parseFloat(document.getElementById('latitudeInput')?.value) || defaultLocation.lat;
        const lngInput = parseFloat(document.getElementById('longitudeInput')?.value) || defaultLocation.lng;
        const initialLocation = [latInput, lngInput];

        map = L.map('map').setView(initialLocation, 12);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap'
        }).addTo(map);

        marker = L.marker(initialLocation, { draggable: true }).addTo(map);

        marker.on('dragend', (e) => {
            const pos = e.target.getLatLng();
            updateCoordinates(pos.lat, pos.lng);
        });

        map.on('click', (e) => {
            marker.setLatLng(e.latlng);
            updateCoordinates(e.latlng.lat, e.latlng.lng);
        });

        console.log('✅ Map initialized successfully');
    } catch (err) {
        console.error('Map initialization error:', err);
    }
}

function updateCoordinates(lat, lng) {
    try {
        document.getElementById('latitudeInput').value = lat.toFixed(6);
        document.getElementById('longitudeInput').value = lng.toFixed(6);
        document.getElementById('googleMapsUrl').value =
            `https://www.openstreetmap.org/?mlat=${lat}&mlon=${lng}#map=18/${lat}/${lng}`;

        const detectedGov = detectGovernorateFromCoordinates(lat, lng);
        if (detectedGov) {
            $('#governorateSelect').val(detectedGov).trigger('change');
            console.log(`✅ Detected: ${governorateBounds[detectedGov].name}`);
            validateSelection(detectedGov, lat, lng);
        }
    } catch (err) {
        console.error('Update coordinates error:', err);
    }
}

window.centerMapToEgypt = function () {
    try {
        if (map && marker) {
            map.setView([defaultLocation.lat, defaultLocation.lng], 12);
            marker.setLatLng(defaultLocation);
            updateCoordinates(defaultLocation.lat, defaultLocation.lng);
        }
    } catch (err) {
        console.error('Center map error:', err);
    }
}

// ✅ USE MY LOCATION BUTTON - CRITICAL FIX
window.useMyLocation = function () {
    console.log('Use my location clicked');
    console.log('Map:', map);
    console.log('Marker:', marker);

    if (!navigator.geolocation) {
        alert("Your browser does not support geolocation.");
        return;
    }

    if (!map || !marker) {
        alert("Map not initialized yet. Please wait.");
        return;
    }

    navigator.geolocation.getCurrentPosition(
        (position) => {
            try {
                const lat = position.coords.latitude;
                const lon = position.coords.longitude;

                console.log(`Got location: ${lat}, ${lon}`);

                marker.setLatLng([lat, lon]);
                map.setView([lat, lon], 16);
                updateCoordinates(lat, lon);

                showToast('Location set successfully!', 'success');
            } catch (err) {
                console.error('Error processing geolocation:', err);
                showToast('Error setting location', 'error');
            }
        },
        (error) => {
            console.error('Geolocation error:', error);
            switch (error.code) {
                case error.PERMISSION_DENIED:
                    alert("Please enable location access in your browser settings.");
                    break;
                case error.POSITION_UNAVAILABLE:
                    alert("Location information is not available.");
                    break;
                case error.TIMEOUT:
                    alert("Location request timed out.");
                    break;
                default:
                    alert("An unknown error occurred.");
                    break;
            }
        },
        { timeout: 10000, enableHighAccuracy: true }
    );
}

// ========================================
// SEARCH FUNCTIONALITY
// ========================================

function initSearch() {
    const searchInput = document.getElementById("placeSearch");
    const resultsBox = document.getElementById("searchResults");

    if (!searchInput || !resultsBox) return;

    let searchTimeout = null;

    searchInput.addEventListener("keyup", function () {
        const query = this.value.trim();
        if (!query) {
            resultsBox.style.display = "none";
            return;
        }

        clearTimeout(searchTimeout);
        resultsBox.innerHTML = '<div class="list-group-item text-center"><i class="fas fa-spinner fa-spin"></i> Searching...</div>';
        resultsBox.style.display = "block";

        searchTimeout = setTimeout(() => {
            fetch(`https://nominatim.openstreetmap.org/search?format=json&limit=8&countrycodes=eg&q=${encodeURIComponent(query)}`, {
                headers: { "User-Agent": "Entertania-App/1.0" }
            })
                .then(res => res.json())
                .then(data => {
                    resultsBox.innerHTML = "";
                    if (!data || data.length === 0) {
                        resultsBox.innerHTML = '<div class="list-group-item text-muted">No results</div>';
                        return;
                    }

                    data.forEach(place => {
                        const item = document.createElement("div");
                        item.className = "list-group-item list-group-item-action";
                        item.style.cursor = "pointer";
                        item.innerHTML = `
                            <div class="d-flex">
                                <i class="fas fa-map-marker-alt text-danger me-2 mt-1"></i>
                                <div>
                                    <div class="fw-bold">${place.display_name.split(",")[0]}</div>
                                    <small class="text-muted">${place.display_name}</small>
                                </div>
                            </div>
                        `;

                        item.addEventListener("click", () => {
                            const lat = parseFloat(place.lat);
                            const lon = parseFloat(place.lon);
                            marker.setLatLng([lat, lon]);
                            map.setView([lat, lon], 16);
                            updateCoordinates(lat, lon);
                            resultsBox.style.display = "none";
                            searchInput.value = '';
                        });

                        resultsBox.appendChild(item);
                    });
                })
                .catch(err => {
                    console.error('Search error:', err);
                    resultsBox.innerHTML = '<div class="list-group-item text-danger">Search error</div>';
                });
        }, 300);
    });
}

// ========================================
// MEDIA PREVIEW
// ========================================

function initPhotoPreview() {
    const photoInput = document.getElementById('photoInput');
    if (!photoInput) return;

    const currentCount = parseInt(photoInput.dataset.currentCount || 0);
    const maxPhotos = 10;

    photoInput.addEventListener('change', function (e) {
        const files = Array.from(e.target.files);
        const previewContainer = document.getElementById('photoPreview');
        if (!previewContainer) return;

        if (files.length === 0) {
            previewContainer.style.display = 'none';
            return;
        }

        if (currentCount + files.length > maxPhotos) {
            alert(`You can only upload ${maxPhotos - currentCount} more photo(s).`);
            e.target.value = '';
            return;
        }

        previewContainer.innerHTML = '';
        previewContainer.style.display = 'flex';

        files.forEach((file) => {
            if (file.type.startsWith('image/')) {
                const reader = new FileReader();
                reader.onload = function (event) {
                    const col = document.createElement('div');
                    col.className = 'col-md-3 col-6';
                    col.innerHTML = `
                        <div class="photo-preview-item">
                            <img src="${event.target.result}" class="img-fluid rounded" alt="Preview" />
                            <small class="d-block text-muted mt-2 text-center text-truncate">${file.name}</small>
                        </div>
                    `;
                    previewContainer.appendChild(col);
                };
                reader.readAsDataURL(file);
            }
        });
    });
}

function initVideoPreview() {
    const videoInput = document.getElementById('videoInput');
    if (!videoInput) return;

    const currentCount = parseInt(videoInput.dataset.currentCount || 0);
    const maxVideos = 5;

    videoInput.addEventListener('change', function (e) {
        const files = Array.from(e.target.files);
        const previewContainer = document.getElementById('videoPreview');
        if (!previewContainer) return;

        if (files.length === 0) {
            previewContainer.style.display = 'none';
            return;
        }

        if (currentCount + files.length > maxVideos) {
            alert(`You can only upload ${maxVideos - currentCount} more video(s).`);
            e.target.value = '';
            return;
        }

        previewContainer.innerHTML = '';
        previewContainer.style.display = 'flex';

        files.forEach((file) => {
            if (file.type.startsWith('video/')) {
                const reader = new FileReader();
                reader.onload = function (event) {
                    const col = document.createElement('div');
                    col.className = 'col-md-3 col-6';
                    col.innerHTML = `
                        <div class="photo-preview-item">
                            <video class="img-fluid rounded" controls preload="metadata">
                                <source src="${event.target.result}" type="${file.type}">
                            </video>
                            <div class="video-indicator">
                                <i class="fas fa-play-circle"></i>
                            </div>
                            <small class="d-block text-muted mt-2 text-center text-truncate">${file.name}</small>
                        </div>
                    `;
                    previewContainer.appendChild(col);
                };
                reader.readAsDataURL(file);
            }
        });
    });
}

// ========================================
// MEDIA MANAGEMENT
// ========================================

async function deletePhoto(photoId) {
    if (!confirm('Delete this photo?')) return;

    try {
        const response = await fetch(`/Venue/DeletePhoto?photoId=${photoId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });

        const result = await response.json();

        if (result.success) {
            const el = document.querySelector(`[data-photo-id="${photoId}"]`);
            if (el) {
                el.style.opacity = '0';
                setTimeout(() => el.remove(), 300);
            }
            showToast('Photo deleted', 'success');
        } else {
            showToast(result.message || 'Failed to delete', 'error');
        }
    } catch (err) {
        console.error('Delete error:', err);
        showToast('Error deleting photo', 'error');
    }
}

async function deleteVideo(videoId) {
    if (!confirm('Delete this video?')) return;

    try {
        const response = await fetch(`/Venue/DeleteVideo?videoId=${videoId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });

        const result = await response.json();

        if (result.success) {
            const el = document.querySelector(`[data-video-id="${videoId}"]`);
            if (el) {
                el.style.opacity = '0';
                setTimeout(() => el.remove(), 300);
            }
            showToast('Video deleted', 'success');
        } else {
            showToast(result.message || 'Failed to delete', 'error');
        }
    } catch (err) {
        console.error('Delete error:', err);
        showToast('Error deleting video', 'error');
    }
}

async function setPrimary(venueId, photoId) {
    try {
        const response = await fetch(`/Venue/SetPrimaryPhoto?venueId=${venueId}&photoId=${photoId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });

        const result = await response.json();

        if (result.success) {
            showToast('Cover photo updated', 'success');
            setTimeout(() => location.reload(), 500);
        } else {
            showToast(result.message || 'Failed', 'error');
        }
    } catch (err) {
        console.error('Error:', err);
        showToast('Error updating photo', 'error');
    }
}

function showToast(message, type = 'info') {
    const bgColor = type === 'success' ? '#28a745' : '#dc3545';
    const toast = document.createElement('div');
    toast.style.cssText = `
        position: fixed;
        bottom: 20px;
        right: 20px;
        background: ${bgColor};
        color: white;
        padding: 12px 16px;
        border-radius: 6px;
        box-shadow: 0 2px 8px rgba(0,0,0,0.2);
        z-index: 9999;
        font-size: 14px;
    `;
    toast.innerHTML = `<i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i> ${message}`;

    document.body.appendChild(toast);

    setTimeout(() => toast.remove(), 3000);
}

// ========================================
// INITIALIZATION
// ========================================

$(document).ready(function () {
    console.log('🚀 Initializing venue form...');

    // Initialize Select2
    const govSelect = $('.select2-governorate');
    if (govSelect.length) {
        govSelect.select2({
            placeholder: 'Select or search governorate...',
            allowClear: false,
            width: '100%'
        });

        // Validate on governorate change
        $('#governorateSelect').on('change', function () {
            const lat = parseFloat($('#latitudeInput').val());
            const lon = parseFloat($('#longitudeInput').val());
            if (lat && lon) {
                validateSelection(parseInt(this.value), lat, lon);
            }
        });
    }

    // Form submission validation
    $('#venueForm').on('submit', function (e) {
        const govSelect = $('#governorateSelect').val();
        const lat = parseFloat($('#latitudeInput').val());
        const lon = parseFloat($('#longitudeInput').val());

        if (!govSelect) {
            e.preventDefault();
            alert('Please select a governorate');
            return false;
        }

        if (lat && lon) {
            const validation = validateGovernorate(parseInt(govSelect), lat, lon);
            if (!validation.valid) {
                e.preventDefault();
                if (!confirm(`Warning: ${validation.message}\n\nContinue anyway?`)) {
                    return false;
                }
            }
        }
    });

    // ✅ USE MY LOCATION BUTTON EVENT LISTENER
    const useLocationBtn = document.getElementById('useMyLocation');
    if (useLocationBtn) {
        useLocationBtn.addEventListener('click', function (e) {
            e.preventDefault();
            useMyLocation();
        });
    }

    // Initialize map and features
    setTimeout(() => {
        initMap();
        initSearch();
        initPhotoPreview();
        initVideoPreview();
        console.log('✅ All features initialized');
    }, 100);
});