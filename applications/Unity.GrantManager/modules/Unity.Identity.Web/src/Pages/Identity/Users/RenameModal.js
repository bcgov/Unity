$(function () {
    const lg = abp.localization.getResource('GrantManager');
    const userImportService = unity.grantManager.identity.userImport;

    const escapeHtml = function (value) {
        return $('<div>').text(value || '').html();
    };

    const comparableDisplayName = function (value) {
        return (value || '').split(':')[0].trim().toLowerCase();
    };

    const showRenameDifferences = function (selectedData) {
        const current = {
            'First name': $('#rename-current-firstName').val() || '',
            'Last name': $('#rename-current-lastName').val() || '',
            'Username': $('#rename-current-userName').val() || '',
            'Email': $('#rename-current-email').val() || '',
            'Display name': $('#rename-current-displayName').val() || '',
            'Badge': $('#rename-current-badge').val() || ''
        };
        const proposed = {
            'First name': selectedData.firstName || '',
            'Last name': selectedData.lastName || '',
            'Username': selectedData.username || '',
            'Email': selectedData.email || '',
            'Display name': selectedData.displayName || '',
            'Badge': ((selectedData.lastName || '').charAt(0) + (selectedData.firstName || '').charAt(0)).toUpperCase()
        };
        const rows = Object.keys(current).map(function (field) {
            return '<tr><th>' + escapeHtml(field) + '</th><td>' + escapeHtml(current[field]) + '</td><td>' + escapeHtml(proposed[field]) + '</td></tr>';
        }).join('');
        const changed = Object.keys(current).some(function (field) {
            if (field === 'Email' && !proposed[field].trim()) {
                return false;
            }
            if (field === 'Display name') {
                return comparableDisplayName(current[field]) !== comparableDisplayName(proposed[field]);
            }
            return current[field].toLowerCase() !== proposed[field].toLowerCase();
        });
        const unchangedMessage = changed ? '' : '<div class="text-info">' + lg('UnityIdentity:UserHasNotChanged') + '</div>';
        $('#rename-differences').html('<table class="table table-sm"><thead><tr><th></th><th>Current</th><th>Directory</th></tr></thead><tbody>' + rows + '</tbody></table>' + unchangedMessage);
        $('#rename-user-btn').prop('disabled', !changed);
    };

    abp.modals.renameUser = function () {
        return {
            initModal: function (publicApi) {
                $('#cancel-rename-btn').click(function () {
                    publicApi.close();
                });

                userImportService.searchForRename({
                    userId: $('#rename-user-id').val(),
                    directory: 'IDIR'
                }).then(function (users) {
                    if (users && users.length === 1) {
                        $('#rename-user-candidate-id').val(users[0].userGuid);
                        showRenameDifferences(users[0]);
                    } else {
                        $('#rename-differences').html('<div class="text-warning">' + lg('UnityIdentity:NoMatchingDirectoryUser') + '</div>');
                    }
                });
            }
        };
    };
});
