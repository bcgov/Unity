/* ApplicantOrganizationInfo Component Scripts */

(function () {
    const sectorDataElement = document.getElementById('ApplicantOrganizationInfo_SectorData');
    const sectorSelect = document.getElementById('ApplicantOrganizationInfo_Sector');
    const subSectorSelect = document.getElementById('ApplicantOrganizationInfo_SubSector');

    if (!sectorDataElement || !sectorSelect || !subSectorSelect) {
        return;
    }

    let sectors = [];

    try {
        const json = sectorDataElement.textContent?.trim() ?? '[]';
        sectors = JSON.parse(json);
    } catch (error) {
        console.warn('Unable to parse ApplicantOrganizationInfo sector data.', error);
        sectors = [];
    }

    const getSectorName = (sector) => sector?.sectorName ?? sector?.SectorName ?? '';
    const getSectorSubSectors = (sector) => sector?.subSectors ?? sector?.SubSectors ?? [];
    const getSubSectorName = (subSector) => subSector?.subSectorName ?? subSector?.SubSectorName ?? '';

    const renderSubSectors = (selectedSector) => {
        const currentValue = subSectorSelect.value;
        subSectorSelect.innerHTML = '';

        const defaultOption = document.createElement('option');
        defaultOption.value = '';
        defaultOption.textContent = 'Please choose...';
        subSectorSelect.appendChild(defaultOption);

        if (!Array.isArray(sectors) || !selectedSector) {
            return;
        }

        const sector = sectors.find(item => getSectorName(item) === selectedSector);
        const subSectors = getSectorSubSectors(sector);

        if (!Array.isArray(subSectors)) {
            return;
        }

        subSectors.forEach(subSector => {
            const option = document.createElement('option');
            option.value = getSubSectorName(subSector);
            option.textContent = getSubSectorName(subSector);
            subSectorSelect.appendChild(option);
        });

        if (Array.from(subSectorSelect.options).some(option => option.value === currentValue)) {
            subSectorSelect.value = currentValue;
        }
    };

    sectorSelect.addEventListener('change', () => renderSubSectors(sectorSelect.value));

    renderSubSectors(sectorSelect.value);
})();
