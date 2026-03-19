(() => {
    const API_BASE = '/api';
    const groups = ['FONDAZIONE SETTORE-1', 'FONDAZIONE SETTORE-2', 'FONDAZIONE SETTORE-3'];

    const state = {
        isAdmin: false,
        matricola: '',
        displayName: '',
        cars: [],
        users: []
    };

    const searchInput = document.getElementById('searchInput');
    const groupFilter = document.getElementById('groupFilter');
    const statusFilter = document.getElementById('statusFilter');
    const ownerFilter = document.getElementById('ownerFilter');
    const fuelFilter = document.getElementById('fuelFilter');
    const ownerNameInput = document.getElementById('mOwnerName');
    const ownerIdInput = document.getElementById('mOwner');
    const ownerHelp = document.getElementById('mOwnerHelp');

    async function requestJson(url, options = {}) {
        const requestOptions = Object.assign({}, options);
        requestOptions.headers = Object.assign({ 'Accept': 'application/json' }, options.headers || {});

        const response = await fetch(url, requestOptions);
        if (response.status === 401) {
            window.location.replace('/Account/Login');
            throw new Error('Sessione non valida.');
        }

        if (!response.ok) {
            const text = await response.text();
            throw new Error('Richiesta fallita (' + response.status + ')\n' + text);
        }

        if (response.status === 204) return null;
        return response.json();
    }

    function setStatus(message, tone = 'info') {
        const box = document.getElementById('statusBox');
        if (!message) {
            box.style.display = 'none';
            box.textContent = '';
            return;
        }

        box.dataset.tone = tone;
        box.style.display = 'block';
        box.textContent = message;
    }

    function safeText(value) {
        return value === null || value === undefined || value === '' ? '-' : String(value);
    }

    function toDateInput(value) {
        return value ? String(value).slice(0, 10) : '';
    }

    function formatDate(value) {
        const normalized = toDateInput(value);
        if (!normalized) return '-';
        const [year, month, day] = normalized.split('-');
        return day + '/' + month + '/' + year;
    }

    function normalizeImage(url) {
        return url && String(url).trim() ? String(url).trim() : 'https://via.placeholder.com/400x250';
    }

    function getCardGroup(car) {
        const group = String(car.gruppo || '').trim().toUpperCase();
        return groups.includes(group) ? group : 'FONDAZIONE SETTORE-3';
    }

    function getButtonClass(group) {
        return getGroupSlug(group);
    }

    function getGroupSlug(group) {
        return String(group || '').trim().toLowerCase().replaceAll(' ', '-');
    }

    function fuelLabel(value) {
        const numeric = Number(value);
        if (Number.isNaN(numeric)) return safeText(value);
        if (numeric >= 2) return 'Alto';
        if (numeric === 1) return 'Medio';
        if (numeric === 0) return 'Riserva';
        return safeText(value);
    }

    function fuelPreset(value) {
        const numeric = Number(value);
        if (Number.isNaN(numeric)) return '1';
        return String(Math.max(0, Math.min(2, numeric)));
    }

    function normalizeStatus(value) {
        return String(value || '').trim().toLowerCase();
    }

    function statusCssClass(value) {
        return 'status-' + normalizeStatus(value).replaceAll(' ', '-');
    }

    function normalizeLookup(value) {
        return String(value || '').trim().toLowerCase();
    }

    function canEditCar(car) {
        return state.isAdmin || String(car.possessoreMatricola) === state.matricola;
    }

    function buildOwnerChoice(user) {
        return user.email ? user.displayName + ' | ' + user.email : user.displayName;
    }

    function getUserByInput(value) {
        const query = normalizeLookup(value);
        if (!query) return null;

        return state.users.find((user) => {
            return normalizeLookup(buildOwnerChoice(user)) === query
                || normalizeLookup(user.displayName) === query
                || normalizeLookup(user.email) === query
                || String(user.id) === query;
        }) || null;
    }

    function ownerDisplay(car) {
        if (car.possessoreNome) {
            return car.possessoreNome;
        }

        if (car.possessoreMatricola) {
            return 'Utente #' + car.possessoreMatricola;
        }

        return '-';
    }

    async function loadSession() {
        const data = await requestJson(API_BASE + '/session/me');
        state.isAdmin = Boolean(data.isAdmin);
        state.matricola = String(data.matricola || '');
        state.displayName = data.displayName || state.matricola;
    }

    async function loadCars() {
        const data = await requestJson(API_BASE + '/fleet/cars');
        state.cars = Array.isArray(data && data.value) ? data.value : [];
    }

    async function loadUsers() {
        if (!state.isAdmin) {
            state.users = [];
            return;
        }

        const data = await requestJson(API_BASE + '/fleet/users');
        state.users = Array.isArray(data && data.value) ? data.value : [];
    }

    function populateGroupSelect() {
        const options = groups.map((group) => '<option value="' + group + '">' + group + '</option>').join('');
        document.getElementById('mCompany').innerHTML = options;
        groupFilter.innerHTML = '<option value="">Tutti i gruppi</option>' + options;
    }

    function populateOwnerSuggestions() {
        const datalist = document.getElementById('ownerOptions');
        datalist.innerHTML = state.users
            .map((user) => '<option value="' + buildOwnerChoice(user).replace(/"/g, '&quot;') + '"></option>')
            .join('');
    }

    function populateSearchFilters() {
        const statuses = [...new Set(state.cars.map((car) => safeText(car.stato)).filter((value) => value !== '-'))].sort();
        const fuels = [...new Set(state.cars.map((car) => fuelLabel(car.fuelLevel)).filter((value) => value !== '-'))];

        const currentStatus = statusFilter.value;
        const currentFuel = fuelFilter.value;

        statusFilter.innerHTML = '<option value="">Tutti gli stati</option>' + statuses.map((status) => '<option value="' + status + '">' + status + '</option>').join('');
        fuelFilter.innerHTML = '<option value="">Tutti i livelli</option>' + fuels.map((fuel) => '<option value="' + fuel + '">' + fuel + '</option>').join('');

        statusFilter.value = statuses.includes(currentStatus) ? currentStatus : '';
        fuelFilter.value = fuels.includes(currentFuel) ? currentFuel : '';
    }

    function renderSummary() {
        const cars = state.cars;
        const inUse = cars.filter((car) => normalizeStatus(car.stato) === 'in uso').length;
        const maintenance = cars.filter((car) => normalizeStatus(car.stato).includes('manutenzione')).length;
        const activeGroups = new Set(cars.map((car) => getCardGroup(car)));

        document.getElementById('totalCount').textContent = String(cars.length);
        document.getElementById('inUseCount').textContent = String(inUse);
        document.getElementById('maintenanceCount').textContent = String(maintenance);
        document.getElementById('groupCount').textContent = String(activeGroups.size);
        document.getElementById('carsCountLabel').textContent = cars.length + (cars.length === 1 ? ' veicolo' : ' veicoli');
    }

    function renderHeader() {
        document.getElementById('headerUser').textContent = state.displayName + (state.isAdmin ? ' | Admin' : ' | Utente');
        document.getElementById('btnAddCar').style.display = state.isAdmin ? 'inline-block' : 'none';
        document.getElementById('debugMenu').style.display = state.isAdmin ? 'block' : 'none';
    }

    function toggleDebugMenu() {
        const dropdown = document.getElementById('debugDropdown');
        dropdown.classList.toggle('open');
    }

    function closeDebugMenu() {
        document.getElementById('debugDropdown').classList.remove('open');
    }

    function renderDeadlineOverview(car) {
        document.getElementById('vRevisioneInizio').textContent = formatDate(car?.revisioneInizio);
        document.getElementById('vRevisioneScadenza').textContent = formatDate(car?.revisioneScadenza || calculateExpiry(car?.revisioneInizio, 2));
        document.getElementById('vBolloInizio').textContent = formatDate(car?.bolloInizio);
        document.getElementById('vBolloScadenza').textContent = formatDate(car?.bolloScadenza || calculateExpiry(car?.bolloInizio, 1));
        document.getElementById('vTagliandoInizio').textContent = formatDate(car?.tagliandoInizio);
        document.getElementById('vTagliandoScadenza').textContent = formatDate(car?.tagliandoScadenza || calculateExpiry(car?.tagliandoInizio, 1));
        document.getElementById('vAssicurazioneInizio').textContent = formatDate(car?.assicurazioneInizio);
        document.getElementById('vAssicurazioneScadenza').textContent = formatDate(car?.assicurazioneScadenza || calculateExpiry(car?.assicurazioneInizio, 1));
    }

    function calculateExpiry(value, years) {
        const normalized = toDateInput(value);
        if (!normalized) return '';
        const date = new Date(normalized + 'T00:00:00');
        date.setFullYear(date.getFullYear() + years);
        return date.toISOString().slice(0, 10);
    }

    function hasMaintenanceRequest(car) {
        return normalizeStatus(car.stato) === 'in richiesta manutenzione';
    }

    function isInMaintenance(car) {
        return normalizeStatus(car.stato) === 'in manutenzione';
    }

    function buildMaintenanceAction(car) {
        if (state.isAdmin && hasMaintenanceRequest(car)) {
            return '<button class="btn-secondary-action" type="button" onclick="approveMaintenance(' + Number(car.id) + ')">Metti in manutenzione</button>';
        }

        if (!state.isAdmin && canEditCar(car) && !hasMaintenanceRequest(car) && !isInMaintenance(car)) {
            return '<button class="btn-secondary-action" type="button" onclick="requestMaintenance(' + Number(car.id) + ')">Segnala manutenzione</button>';
        }

        return '';
    }

    function buildUseAction(car, buttonClass) {
        if (!state.isAdmin && canEditCar(car) && !hasMaintenanceRequest(car) && !isInMaintenance(car)) {
            const label = normalizeStatus(car.stato) === 'in uso' ? 'Smetti di usare' : 'Utilizza ora';
            return '<button class="btn-use-now btn-' + buttonClass + '" type="button" onclick="useNow(' + Number(car.id) + ')">' + label + '</button>';
        }

        return '';
    }

    function searchScore(car, tokens) {
        const values = [
            String(car.modello || '').toLowerCase(),
            String(car.targa || '').toLowerCase(),
            String(car.possessoreNome || '').toLowerCase()
        ];
        let score = 0;

        tokens.forEach((token) => {
            values.forEach((value, index) => {
                if (value === token) score += index === 0 ? 6 : 4;
                else if (value.includes(token)) score += index === 0 ? 4 : 2;
            });
        });

        return score;
    }

    function getFilteredCars() {
        const query = searchInput.value.trim().toLowerCase();
        const tokens = query ? query.split(/\s+/).filter(Boolean) : [];
        const group = groupFilter.value;
        const status = statusFilter.value;
        const owner = ownerFilter.value.trim().toLowerCase();
        const fuel = fuelFilter.value;

        return state.cars
            .filter((car) => {
                const ownerText = [car.possessoreNome, car.possessoreMatricola].join(' ').toLowerCase();
                const haystack = [
                    car.modello,
                    car.targa,
                    car.possessoreNome,
                    car.possessoreMatricola,
                    car.gruppo,
                    car.stato,
                    car.fuelType,
                    fuelLabel(car.fuelLevel)
                ].join(' ').toLowerCase();

                const matchesTokens = !tokens.length || tokens.every((token) => haystack.includes(token));
                const matchesGroup = !group || getCardGroup(car) === group;
                const matchesStatus = !status || safeText(car.stato) === status;
                const matchesOwner = !owner || ownerText.includes(owner);
                const matchesFuel = !fuel || fuelLabel(car.fuelLevel) === fuel;

                return matchesTokens && matchesGroup && matchesStatus && matchesOwner && matchesFuel;
            })
            .sort((a, b) => searchScore(b, tokens) - searchScore(a, tokens) || String(a.targa || '').localeCompare(String(b.targa || '')));
    }

    function renderCars() {
        const container = document.getElementById('carsContainer');
        const filteredCars = getFilteredCars();
        let html = '';

        groups.forEach((group) => {
            const groupCars = filteredCars.filter((car) => getCardGroup(car) === group);
            if (!groupCars.length) return;

            html += '<h3 class="group-header title-' + getGroupSlug(group) + '">' + group + '</h3>';
            html += '<div class="cars-grid">';
            html += groupCars.map((car) => {
                const buttonClass = getButtonClass(group);
                return [
                    '<div class="car-card card-' + buttonClass + '">',
                    '<div class="car-image" style="background-image:url(\'' + normalizeImage(car.imageUrl).replace(/'/g, '%27') + '\')"></div>',
                    '<div class="car-info">',
                    '<div class="car-model">' + safeText(car.modello) + '</div>',
                    '<div class="car-brand ' + statusCssClass(car.stato) + '">' + safeText(car.stato) + '</div>',
                    '<div class="car-targa">Targa: ' + safeText(car.targa) + '</div>',
                    '<div class="car-detail"><span>Assegnatario:</span><strong>' + ownerDisplay(car) + '</strong></div>',
                    '<div class="car-detail"><span>KM:</span><strong>' + Number(car.chilometraggio || 0).toLocaleString('it-IT') + '</strong></div>',
                    '<div class="car-detail"><span>Carburante:</span><strong>' + fuelLabel(car.fuelLevel) + '</strong></div>',
                    buildUseAction(car, buttonClass),
                    buildMaintenanceAction(car),
                    '<button class="btn-edit btn-' + buttonClass + '" type="button" onclick="openModal(\'edit\', ' + Number(car.id) + ')">' + (canEditCar(car) ? 'Modifica' : 'Dettagli') + '</button>',
                    '</div>',
                    '</div>'
                ].join('');
            }).join('');
            html += '</div>';
        });

        container.innerHTML = html || '<div class="empty-state">Nessun veicolo trovato con i filtri correnti.</div>';
    }

    function updateOwnerHelper() {
        if (!state.isAdmin) {
            ownerHelp.textContent = '';
            return;
        }

        const selectedUser = getUserByInput(ownerNameInput.value);
        if (selectedUser) {
            ownerIdInput.value = String(selectedUser.id);
            ownerHelp.textContent = 'Assegnerai il veicolo a ' + selectedUser.displayName + ' (#' + selectedUser.id + ').';
            return;
        }

        ownerIdInput.value = '';
        ownerHelp.textContent = ownerNameInput.value.trim()
            ? 'Il backend provera a risolvere automaticamente il nome inserito.'
            : 'Scrivi nome o email e scegli uno dei suggerimenti.';
    }

    function resetForm() {
        document.getElementById('editId').value = '';
        document.getElementById('mModel').value = '';
        document.getElementById('mTarga').value = '';
        ownerIdInput.value = '';
        ownerNameInput.value = '';
        document.getElementById('mCompany').value = 'FONDAZIONE SETTORE-1';
        document.getElementById('mKm').value = '';
        document.getElementById('mFuel').value = '2';
        document.getElementById('mFuelType').value = '';
        document.getElementById('mStatus').value = 'in uso';
        document.getElementById('mPossesso').value = '';
        document.getElementById('mImg').value = '';
        document.getElementById('mRevisioneInizio').value = '';
        document.getElementById('mBolloInizio').value = '';
        document.getElementById('mTagliandoInizio').value = '';
        document.getElementById('mAssicurazioneInizio').value = '';
        renderDeadlineOverview(null);
        updateOwnerHelper();
    }

    function syncPermissions(editingCar) {
        const isEdit = Boolean(editingCar);
        const allowSensitive = state.isAdmin;
        const canEdit = state.isAdmin || !isEdit || canEditCar(editingCar);

        ['mModel', 'mTarga', 'mKm', 'mFuel', 'mFuelType', 'mPossesso', 'mImg', 'mRevisioneInizio', 'mBolloInizio', 'mTagliandoInizio', 'mAssicurazioneInizio']
            .forEach((id) => {
                document.getElementById(id).disabled = !canEdit;
            });

        document.getElementById('mStatus').disabled = !state.isAdmin || !canEdit;
        ownerNameInput.disabled = !allowSensitive;
        document.getElementById('mCompany').disabled = !allowSensitive;
        document.getElementById('btnDelete').style.display = state.isAdmin && isEdit ? 'block' : 'none';
    }

    function openModal(mode, id) {
        const isEdit = mode === 'edit';
        const modal = document.getElementById('carModal');
        const modalTitle = document.getElementById('modalTitle');

        resetForm();
        modal.style.display = 'flex';

        if (!isEdit) {
            if (!state.isAdmin) {
                setStatus('Solo un amministratore puo aggiungere nuovi veicoli.', 'error');
                closeModal();
                return;
            }

            modalTitle.textContent = 'Aggiungi Nuovo Veicolo';
            syncPermissions(null);
            return;
        }

        const car = state.cars.find((item) => Number(item.id) === Number(id));
        if (!car) {
            setStatus('Veicolo non trovato.', 'error');
            closeModal();
            return;
        }

        modalTitle.textContent = canEditCar(car) ? 'Modifica Veicolo' : 'Dettagli Veicolo';
        document.getElementById('editId').value = car.id;
        document.getElementById('mModel').value = car.modello || '';
        document.getElementById('mTarga').value = car.targa || '';
        ownerIdInput.value = car.possessoreMatricola || '';
        ownerNameInput.value = car.possessoreNome
            ? [car.possessoreNome, state.users.find((user) => Number(user.id) === Number(car.possessoreMatricola))?.email].filter(Boolean).join(' | ')
            : '';
        document.getElementById('mCompany').value = getCardGroup(car);
        document.getElementById('mKm').value = car.chilometraggio || '';
        document.getElementById('mFuel').value = fuelPreset(car.fuelLevel);
        document.getElementById('mFuelType').value = car.fuelType || '';
        document.getElementById('mStatus').value = car.stato || 'in uso';
        document.getElementById('mPossesso').value = toDateInput(car.dataPossesso);
        document.getElementById('mImg').value = car.imageUrl || '';
        document.getElementById('mRevisioneInizio').value = toDateInput(car.revisioneInizio);
        document.getElementById('mBolloInizio').value = toDateInput(car.bolloInizio);
        document.getElementById('mTagliandoInizio').value = toDateInput(car.tagliandoInizio);
        document.getElementById('mAssicurazioneInizio').value = toDateInput(car.assicurazioneInizio);

        renderDeadlineOverview(car);
        updateOwnerHelper();
        syncPermissions(car);
    }

    function closeModal() {
        document.getElementById('carModal').style.display = 'none';
    }

    function buildPayload() {
        updateOwnerHelper();

        return {
            modello: document.getElementById('mModel').value.trim(),
            targa: document.getElementById('mTarga').value.trim().toUpperCase(),
            possessoreMatricola: ownerIdInput.value ? Number(ownerIdInput.value) : null,
            ownerQuery: ownerNameInput.value.trim(),
            gruppo: document.getElementById('mCompany').value,
            chilometraggio: document.getElementById('mKm').value ? Number(document.getElementById('mKm').value) : 0,
            fuelLevel: Number(document.getElementById('mFuel').value),
            fuelType: document.getElementById('mFuelType').value.trim(),
            stato: document.getElementById('mStatus').value,
            dataPossesso: document.getElementById('mPossesso').value || null,
            imageUrl: document.getElementById('mImg').value.trim() || 'https://via.placeholder.com/400x250',
            revisioneInizio: document.getElementById('mRevisioneInizio').value || null,
            bolloInizio: document.getElementById('mBolloInizio').value || null,
            tagliandoInizio: document.getElementById('mTagliandoInizio').value || null,
            assicurazioneInizio: document.getElementById('mAssicurazioneInizio').value || null
        };
    }

    function validatePayload(payload, isEdit) {
        if (!payload.modello || !payload.targa || !Number.isInteger(payload.chilometraggio)) {
            throw new Error('Compila i campi obbligatori: Modello, Targa e Km.');
        }

        if (state.isAdmin && !payload.ownerQuery && !Number.isInteger(payload.possessoreMatricola)) {
            throw new Error('Per l\'admin l\'assegnatario e obbligatorio.');
        }

        if (!isEdit && !state.isAdmin) {
            throw new Error('Solo l\'admin puo creare nuovi veicoli.');
        }
    }

    async function saveCar() {
        const id = document.getElementById('editId').value;
        const isEdit = Boolean(id);
        const payload = buildPayload();

        try {
            validatePayload(payload, isEdit);

            let url = API_BASE + '/fleet/cars';
            let method = 'POST';

            if (isEdit) {
                method = 'PATCH';
                url += '/' + Number(id);
            }

            await requestJson(url, {
                method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            await loadCars();
            populateSearchFilters();
            renderSummary();
            renderCars();
            if (isEdit) {
                const updatedCar = state.cars.find((item) => Number(item.id) === Number(id));
                if (updatedCar) {
                    renderDeadlineOverview(updatedCar);
                }
            }
            closeModal();
            setStatus('Dati del veicolo salvati correttamente.');
        } catch (error) {
            setStatus(error.message || 'Errore durante il salvataggio.', 'error');
        }
    }

    async function deleteCar() {
        if (!state.isAdmin) {
            setStatus('Solo un amministratore puo eliminare veicoli.', 'error');
            return;
        }

        const id = document.getElementById('editId').value;
        if (!id) return;
        if (!confirm('Eliminare definitivamente questo veicolo?')) return;

        try {
            await requestJson(API_BASE + '/fleet/cars/' + Number(id), { method: 'DELETE' });
            await loadCars();
            populateSearchFilters();
            renderSummary();
            renderCars();
            closeModal();
            setStatus('Veicolo eliminato correttamente.');
        } catch (error) {
            setStatus(error.message || 'Errore durante l\'eliminazione.', 'error');
        }
    }

    async function requestMaintenance(id) {
        if (!confirm('Vuoi segnalare questo veicolo per manutenzione?')) {
            return;
        }

        try {
            await requestJson(API_BASE + '/fleet/cars/' + Number(id) + '/maintenance-request', { method: 'PATCH' });
            await loadCars();
            populateSearchFilters();
            renderSummary();
            renderCars();
            setStatus('Segnalazione manutenzione inviata.');
        } catch (error) {
            setStatus(error.message || 'Errore durante la segnalazione.', 'error');
        }
    }

    async function useNow(id) {
        const car = state.cars.find((item) => Number(item.id) === Number(id));
        const isUsing = car && normalizeStatus(car.stato) === 'in uso';
        const confirmationText = isUsing
            ? 'Vuoi smettere di usare questo veicolo?'
            : 'Vuoi utilizzare questo veicolo adesso?';

        if (!confirm(confirmationText)) {
            return;
        }

        try {
            await requestJson(API_BASE + '/fleet/cars/' + Number(id) + '/use-now', { method: 'PATCH' });
            await loadCars();
            populateSearchFilters();
            renderSummary();
            renderCars();
            setStatus('Stato veicolo aggiornato.');
        } catch (error) {
            setStatus(error.message || 'Errore durante l\'attivazione del veicolo.', 'error');
        }
    }

    async function approveMaintenance(id) {
        try {
            await requestJson(API_BASE + '/fleet/cars/' + Number(id) + '/maintenance-approval', { method: 'PATCH' });
            await loadCars();
            populateSearchFilters();
            renderSummary();
            renderCars();
            setStatus('Veicolo impostato in manutenzione.');
        } catch (error) {
            setStatus(error.message || 'Errore durante l\'aggiornamento della manutenzione.', 'error');
        }
    }

    async function seedDemoData() {
        if (!state.isAdmin) return;
        if (!confirm('Vuoi inserire un nuovo set di veicoli demo realistici?')) {
            return;
        }

        try {
            const response = await requestJson(API_BASE + '/fleet/debug/seed', { method: 'POST' });
            await loadCars();
            populateSearchFilters();
            renderSummary();
            renderCars();
            closeDebugMenu();
            setStatus((response?.inserted || 0) + ' veicoli demo inseriti correttamente.');
        } catch (error) {
            setStatus(error.message || 'Errore durante il caricamento dei dati demo.', 'error');
        }
    }

    async function clearDemoData() {
        if (!state.isAdmin) return;
        if (!confirm('Vuoi cancellare tutti i veicoli e i dati collegati del parco auto?')) {
            return;
        }

        try {
            await requestJson(API_BASE + '/fleet/debug/clear', { method: 'DELETE' });
            await loadCars();
            populateSearchFilters();
            renderSummary();
            renderCars();
            closeDebugMenu();
            setStatus('Parco auto svuotato correttamente.');
        } catch (error) {
            setStatus(error.message || 'Errore durante la cancellazione dei dati.', 'error');
        }
    }

    async function bootstrap() {
        document.getElementById('appShell').style.display = 'block';
        populateGroupSelect();
        setStatus('Caricamento dati in corso...');

        try {
            await loadSession();
            await loadUsers();
            populateOwnerSuggestions();
            await loadCars();
            populateSearchFilters();
            renderHeader();
            renderSummary();
            renderCars();
            setStatus('');
        } catch (error) {
            setStatus(error.message || 'Errore durante il caricamento.', 'error');
        }
    }

    searchInput.addEventListener('input', renderCars);
    groupFilter.addEventListener('change', renderCars);
    statusFilter.addEventListener('change', renderCars);
    ownerFilter.addEventListener('input', renderCars);
    fuelFilter.addEventListener('change', renderCars);
    ownerNameInput.addEventListener('input', updateOwnerHelper);
    ownerNameInput.addEventListener('blur', updateOwnerHelper);

    window.openModal = openModal;
    window.closeModal = closeModal;
    window.saveCar = saveCar;
    window.deleteCar = deleteCar;
    window.requestMaintenance = requestMaintenance;
    window.approveMaintenance = approveMaintenance;
    window.useNow = useNow;
    window.seedDemoData = seedDemoData;
    window.clearDemoData = clearDemoData;
    window.toggleDebugMenu = toggleDebugMenu;
    window.logout = () => { window.location.href = '/Account/Logout'; };

    window.addEventListener('click', (event) => {
        const modal = document.getElementById('carModal');
        if (event.target === modal) {
            closeModal();
        }

        const debugMenu = document.getElementById('debugMenu');
        if (debugMenu && !debugMenu.contains(event.target)) {
            closeDebugMenu();
        }
    });

    window.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') {
            closeModal();
        }
    });

    bootstrap();
})();
