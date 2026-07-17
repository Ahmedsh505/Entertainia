// EventEdit.js - Event Edit Page JavaScript

let discountCodeIndex = 0;
const currentEventId = document.querySelector('input[name="Event.EventId"]')?.value || null;

document.addEventListener("DOMContentLoaded", function () {
    const categorySelect = document.getElementById("categorySelect");
    const subContainer = document.getElementById("subEventContainer");

    // Set discount code index from existing codes
    const existingDiscounts = document.querySelectorAll('.discount-code-item').length;
    if (existingDiscounts > 0) {
        discountCodeIndex = existingDiscounts;
    }

    // Load existing category form if category is set
    const currentCategory = categorySelect.value;
    if (currentCategory) {
        loadCategoryForm(currentCategory);
    }

    // Handle category change
    categorySelect.addEventListener("change", function () {
        const category = this.value;

        if (!category) {
            subContainer.innerHTML = "";
            return;
        }

        loadCategoryForm(category);
    });

    function loadCategoryForm(category) {
        const eventId = document.querySelector('input[name="Event.EventId"]').value;

        fetch(`/Events/LoadCategoryFormForEdit?category=${category}&eventId=${eventId}`)
            .then(res => res.text())
            .then(html => {
                subContainer.innerHTML = html;
            })
            .catch(err => {
                console.error('Error loading form:', err);
                subContainer.innerHTML = "<p class='text-danger'>Error loading form</p>";
            });
    }

    // Initialize venue availability
    initializeVenueAvailability();
});

// Discount code management
function addDiscountCode() {
    const container = document.getElementById('discountCodesContainer');
    const newCode = `
        <div class="discount-code-item border p-3 mb-3 rounded bg-light">
            <div class="row">
                <div class="col-md-3 mb-2">
                    <label class="form-label">Code <span class="text-danger">*</span></label>
                    <input name="DiscountCodes[${discountCodeIndex}].Code"
                           class="form-control"
                           placeholder="SAVE20"
                           required />
                </div>
                <div class="col-md-2 mb-2">
                    <label class="form-label">Type</label>
                    <select name="DiscountCodes[${discountCodeIndex}].DiscountType" class="form-control">
                        <option value="0">Percentage</option>
                        <option value="1">Fixed Amount</option>
                    </select>
                </div>
                <div class="col-md-2 mb-2">
                    <label class="form-label">Value <span class="text-danger">*</span></label>
                    <input name="DiscountCodes[${discountCodeIndex}].DiscountValue"
                           class="form-control"
                           type="number"
                           step="0.01"
                           min="0.01"
                           placeholder="20"
                           required />
                </div>
                <div class="col-md-2 mb-2">
                    <label class="form-label">Valid From</label>
                    <input name="DiscountCodes[${discountCodeIndex}].ValidFrom"
                           class="form-control"
                           type="datetime-local" />
                </div>
                <div class="col-md-2 mb-2">
                    <label class="form-label">Valid Until</label>
                    <input name="DiscountCodes[${discountCodeIndex}].ValidUntil"
                           class="form-control"
                           type="datetime-local" />
                </div>
                <div class="col-md-1 mb-2 d-flex align-items-end">
                    <button type="button"
                            class="btn btn-danger btn-sm w-100"
                            onclick="removeDiscountCode(this)"
                            title="Remove">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>
            <div class="row">
                <div class="col-md-3 mb-2">
                    <label class="form-label">Max Usage</label>
                    <input name="DiscountCodes[${discountCodeIndex}].MaxUsageCount"
                           class="form-control"
                           type="number"
                           min="1"
                           placeholder="Unlimited" />
                    <small class="text-muted">Leave blank for unlimited</small>
                </div>
                <div class="col-md-3 mb-2">
                    <div class="form-check mt-4">
                        <input name="DiscountCodes[${discountCodeIndex}].IsActive"
                               class="form-check-input"
                               type="checkbox"
                               value="true"
                               checked
                               id="discount_active_${discountCodeIndex}" />
                        <label class="form-check-label" for="discount_active_${discountCodeIndex}">
                            Is Active
                        </label>
                    </div>
                </div>
            </div>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', newCode);
    discountCodeIndex++;
}

function removeDiscountCode(button) {
    if (confirm('Are you sure you want to remove this discount code?')) {
        button.closest('.discount-code-item').remove();
    }
}

// Photo management
async function deletePhoto(photoId) {
    if (!confirm('Delete this photo?')) return;

    try {
        const response = await fetch('/Events/DeletePhoto?photoId=' + photoId, {
            method: 'POST'
        });

        const result = await response.json();
        if (result.success) {
            location.reload();
        } else {
            alert(result.message);
        }
    } catch (error) {
        console.error('Error deleting photo:', error);
        alert('An error occurred while deleting the photo');
    }
}

async function setPrimary(eventId, photoId) {
    try {
        const response = await fetch('/Events/SetPrimaryPhoto?eventId=' + eventId + '&photoId=' + photoId, {
            method: 'POST'
        });

        const result = await response.json();
        if (result.success) {
            location.reload();
        } else {
            alert(result.message);
        }
    } catch (error) {
        console.error('Error setting primary photo:', error);
        alert('An error occurred while setting the primary photo');
    }
}

// Video management
async function deleteVideo(videoId) {
    if (!confirm('Delete this video?')) return;

    try {
        const response = await fetch('/Events/DeleteEventVideo?videoId=' + videoId, {
            method: 'POST'
        });

        const result = await response.json();
        if (result.success) {
            location.reload();
        } else {
            alert(result.message);
        }
    } catch (error) {
        console.error('Error deleting video:', error);
        alert('An error occurred while deleting the video');
    }
}

// Venue availability management
function initializeVenueAvailability() {
    const venueSelect = document.querySelector('[name="Event.VenueId"]');
    const startInput = document.querySelector('[name="Event.StartDate"]');
    const endInput = document.querySelector('[name="Event.EndDate"]');

    if (!venueSelect || !startInput || !endInput) return;

    let bookedSlots = [];
    let currentVenueId = venueSelect.value;

    function isDateInBookedSlot(date) {
        const checkTime = date.getTime();
        return bookedSlots.some(slot => {
            const slotStart = new Date(slot.from).getTime();
            const slotEnd = new Date(slot.to).getTime();
            return checkTime >= slotStart && checkTime <= slotEnd;
        });
    }

    const startPicker = flatpickr(startInput, {
        enableTime: true,
        dateFormat: "Y-m-d H:i",
        time_24hr: true,
        minuteIncrement: 30,
        defaultDate: startInput.value,
        onDayCreate: function (dObj, dStr, fp, dayElem) {
            const date = dayElem.dateObj;
            if (isDateInBookedSlot(date)) {
                dayElem.classList.add('booked-date');
                dayElem.title = 'This date has booked events';
            }
        },
        onChange: function (selectedDates, dateStr, instance) {
            if (selectedDates.length > 0) {
                const selectedDate = selectedDates[0];

                if (isDateInBookedSlot(selectedDate)) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Time Unavailable',
                        text: 'This venue is already booked at the selected time. Please choose a different date/time.',
                        confirmButtonColor: '#d33'
                    });
                    instance.clear();
                    return;
                }

                if (endPicker) {
                    endPicker.set('minDate', selectedDate);
                }
            }
        }
    });

    const endPicker = flatpickr(endInput, {
        enableTime: true,
        dateFormat: "Y-m-d H:i",
        time_24hr: true,
        minuteIncrement: 30,
        defaultDate: endInput.value,
        onDayCreate: function (dObj, dStr, fp, dayElem) {
            const date = dayElem.dateObj;
            if (isDateInBookedSlot(date)) {
                dayElem.classList.add('booked-date');
                dayElem.title = 'This date has booked events';
            }
        },
        onChange: function (selectedDates, dateStr, instance) {
            if (selectedDates.length > 0) {
                const selectedDate = selectedDates[0];

                if (isDateInBookedSlot(selectedDate)) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Time Unavailable',
                        text: 'This venue is already booked at the selected time. Please choose a different date/time.',
                        confirmButtonColor: '#d33'
                    });
                    instance.clear();
                    return;
                }

                const startDate = startPicker.selectedDates[0];
                if (startDate && selectedDate <= startDate) {
                    Swal.fire({
                        icon: 'warning',
                        title: 'Invalid Date Range',
                        text: 'End date must be after start date.',
                        confirmButtonColor: '#f39c12'
                    });
                    instance.clear();
                }
            }
        }
    });

    // Load availability for current venue on page load
    if (currentVenueId) {
        loadVenueAvailability(currentVenueId, startPicker, endPicker);
    }

    // Handle venue change
    venueSelect.addEventListener("change", function () {
        const venueId = this.value;
        currentVenueId = venueId;

        startPicker.clear();
        endPicker.clear();
        bookedSlots = [];

        if (!venueId) return;

        loadVenueAvailability(venueId, startPicker, endPicker);
    });

    function loadVenueAvailability(venueId, startPicker, endPicker) {
        fetch(`/Events/GetVenueAvailability?venueId=${venueId}`)
            .then(res => res.json())
            .then(data => {
                // Filter out current event's booking
                bookedSlots = data
                    .filter(ev => {
                        const eventStart = new Date(ev.start);
                        const eventEnd = new Date(ev.end);
                        const currentStart = new Date(startInput.value);
                        const currentEnd = new Date(endInput.value);

                        // Exclude if this is the current event's slot
                        const isSameSlot = Math.abs(eventStart - currentStart) < 60000 &&
                            Math.abs(eventEnd - currentEnd) < 60000;
                        return !isSameSlot;
                    })
                    .map(ev => ({
                        from: new Date(ev.start),
                        to: new Date(ev.end)
                    }));

                startPicker.redraw();
                endPicker.redraw();

                if (bookedSlots.length > 0) {
                    Swal.fire({
                        icon: 'info',
                        title: 'Venue Selected',
                        html: `
                            <p>This venue has ${bookedSlots.length} other booked event(s).</p>
                            <p class="text-muted small">Dates with existing bookings will be highlighted in red and cannot be selected.</p>
                        `,
                        confirmButtonColor: '#3085d6',
                        timer: 4000
                    });
                } else {
                    Swal.fire({
                        icon: 'success',
                        title: 'Venue Available',
                        text: 'This venue has no conflicting bookings. You can select any date.',
                        confirmButtonColor: '#28a745',
                        timer: 3000
                    });
                }
            })
            .catch(err => {
                console.error("Error fetching availability:", err);
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: 'Failed to load venue availability. Please try again.',
                    confirmButtonColor: '#d33'
                });
            });
    }

    // Form validation before submit
    document.getElementById('eventForm').addEventListener('submit', function (e) {
        const startDate = startPicker.selectedDates[0];
        const endDate = endPicker.selectedDates[0];

        if (startDate && isDateInBookedSlot(startDate)) {
            e.preventDefault();
            Swal.fire({
                icon: 'error',
                title: 'Invalid Start Date',
                text: 'The selected start date/time conflicts with an existing booking.',
                confirmButtonColor: '#d33'
            });
            return false;
        }

        if (endDate && isDateInBookedSlot(endDate)) {
            e.preventDefault();
            Swal.fire({
                icon: 'error',
                title: 'Invalid End Date',
                text: 'The selected end date/time conflicts with an existing booking.',
                confirmButtonColor: '#d33'
            });
            return false;
        }

        if (startDate && endDate) {
            const hasConflict = bookedSlots.some(slot => {
                const slotStart = new Date(slot.from).getTime();
                const slotEnd = new Date(slot.to).getTime();
                const eventStart = startDate.getTime();
                const eventEnd = endDate.getTime();
                return (eventStart <= slotEnd && eventEnd >= slotStart);
            });

            if (hasConflict) {
                e.preventDefault();
                Swal.fire({
                    icon: 'error',
                    title: 'Schedule Conflict',
                    text: 'Your event time range overlaps with an existing booking. Please choose different dates.',
                    confirmButtonColor: '#d33'
                });
                return false;
            }
        }
    });
}