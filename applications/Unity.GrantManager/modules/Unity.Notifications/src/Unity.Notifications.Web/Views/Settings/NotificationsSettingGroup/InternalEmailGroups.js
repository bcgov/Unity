
let emailGroupsTable = null;

const emailGroupsManager = {
    initialized: false,

    // Shared utility functions
    utils: {
        // Standard user table column definitions
        getUserTableColumnDefs: function() {
            return [
                {
                    title: "First Name",
                    name: 'firstName',
                    data: 'firstName',
                    defaultContent: ''
                },
                {
                    title: "Last Name",
                    name: 'lastName',
                    data: 'lastName',
                    defaultContent: ''
                },
                {
                    title: "Display Name",
                    name: 'userName',
                    data: 'userName'
                },
                {
                    title: "",
                    name: 'actions',
                    data: null,
                    orderable: false,
                    className: "text-center",
                    render: function (data, type, row) {
                        const title = 'Remove user';
                        const buttonClass = row.isNew ? 'remove-selected-user' : 'remove-user-btn';
                        const dataAttr = row.isNew ? `data-user-id="${row.userId}"` : `data-group-user-id="${row.id}"`;
                        return `<button class="btn btn-sm btn-link text-primary p-0 ${buttonClass}" style="text-decoration: none;" ${dataAttr} title="${title}">
                            <i class="fa fa-times"></i>
                        </button>`;
                    }
                }
            ];
        },

        // Standard DataTable configuration
        getStandardDataTableConfig: function(data = []) {
            return {
                processing: false,
                serverSide: false,
                paging: true,
                pageLength: 10,
                lengthMenu: [[5, 10, 25, 50, -1], [5, 10, 25, 50, "All"]],
                searching: true,
                info: true,
                ordering: true,
                scrollCollapse: false,
                scrollX: false,
                autoWidth: false,
                data: data,
                columnDefs: this.getUserTableColumnDefs()
            };
        },

        // Generate dropdown item HTML for users
        generateUserDropdownItem: function(user) {
            const firstName = user.name ? user.name.split(' ')[0] : '';
            const lastName = user.surname || '';
            return `
                <li>
                    <a class="dropdown-item dropdown-user-item" href="#" 
                        data-user-id="${user.id}" 
                        data-user-name="${user.userName || user.name || 'Unknown'}"
                        data-user-email="${user.email || ''}"
                        data-first-name="${firstName}"
                        data-last-name="${lastName}">
                        <div>
                            <strong>${user.userName || user.name || 'Unknown'}</strong>
                            <small class="text-muted d-block">${user.email || ''}</small>
                        </div>
                    </a>
                </li>
            `;
        },

        // Filter users by search term
        filterUsersBySearchTerm: function(allUsers, searchTerm) {
            return allUsers.filter(user => {
                const userName = (user.userName || user.name || '').toLowerCase();
                const email = (user.email || '').toLowerCase();
                return userName.includes(searchTerm) || email.includes(searchTerm);
            });
        },

        // Setup user search dropdown functionality
        setupUserSearchDropdown: function(searchInputId, dropdownId, addButtonId, onUserSelected) {
            const self = this;
            let searchTimeout;
            let allUsers = [];
            let selectedUser = null;

            // Load all users initially
            emailGroupsManager.loadAllUsers(function (users) {
                allUsers = users;
            });

            // Filter users as user types
            $(`#${searchInputId}`).on('input', function () {
                clearTimeout(searchTimeout);
                const searchTerm = $(this).val().toLowerCase();

                // Show dropdown if not already shown
                if (!$(`#${dropdownId}`).hasClass('show')) {
                    $(`#${dropdownId}`).addClass('show');
                    $(this).attr('aria-expanded', 'true');
                }

                searchTimeout = setTimeout(() => {
                    if (searchTerm.length > 0) {
                        const filteredUsers = self.filterUsersBySearchTerm(allUsers, searchTerm);
                        self.displayFilteredUsers(dropdownId, filteredUsers);
                    } else {
                        $(`#${dropdownId}`).html('<li class="px-3 py-2 text-muted">Start typing to search users...</li>');
                    }
                }, 300);
            });

            // Handle selecting user from dropdown
            $(document).on('click', `#${dropdownId} .dropdown-user-item`, function (e) {
                e.preventDefault();
                e.stopPropagation();
                const userId = $(this).data('user-id');
                const userName = $(this).data('user-name');
                const userEmail = $(this).data('user-email');

                // Store selected user with all details
                selectedUser = {
                    userId,
                    userName,
                    userEmail,
                    firstName: $(this).data('first-name') || '',
                    lastName: $(this).data('last-name') || ''
                };

                // Update search input with selected user
                $(`#${searchInputId}`).val(userName);

                // Enable the Add User button
                $(`#${addButtonId}`).prop('disabled', false);

                // Hide dropdown
                $(`#${dropdownId}`).removeClass('show');
                $(`#${searchInputId}`).attr('aria-expanded', 'false');
            });

            // Handle Add User button click
            $(`#${addButtonId}`).on('click', function () {
                if (selectedUser && onUserSelected) {
                    onUserSelected(selectedUser);

                    // Reset state
                    selectedUser = null;
                    $(`#${searchInputId}`).val('');
                    $(`#${addButtonId}`).prop('disabled', true);
                    $(`#${dropdownId}`).html('<li class="px-3 py-2 text-muted">Start typing to search users...</li>');
                }
            });

            // Clear selection when search input changes
            $(`#${searchInputId}`).on('input', function () {
                const inputVal = $(this).val();
                if (!inputVal || (selectedUser && inputVal !== selectedUser.userName)) {
                    selectedUser = null;
                    $(`#${addButtonId}`).prop('disabled', true);
                }
            });

            return {
                getSelectedUser: () => selectedUser,
                clearSelection: () => {
                    selectedUser = null;
                    $(`#${searchInputId}`).val('');
                    $(`#${addButtonId}`).prop('disabled', true);
                    $(`#${dropdownId}`).html('<li class="px-3 py-2 text-muted">Start typing to search users...</li>');
                }
            };
        },

        // Display filtered users in dropdown
        displayFilteredUsers: function(dropdownId, filteredUsers, excludeUserIds = []) {
            const availableUsers = filteredUsers.filter(user => !excludeUserIds.includes(user.id));
            
            if (availableUsers.length === 0) {
                $(`#${dropdownId}`).html('<li class="px-3 py-2 text-muted">No matching users found</li>');
                return;
            }

            const itemsHtml = availableUsers.slice(0, 20).map(user => 
                this.generateUserDropdownItem(user)
            ).join('');

            $(`#${dropdownId}`).html(itemsHtml);
        }
    },

    init: function () {
        if (this.initialized) {
            if (emailGroupsTable) {
                emailGroupsTable.ajax.reload();
            }
            return;
        }
        this.initializeDataTable();
        this.bindEvents();
        this.initialized = true;
    },

    initializeDataTable: function () {
        const self = this;

        emailGroupsTable = $('#EmailGroupsTable').DataTable(abp.libs.datatables.normalizeConfiguration({
            processing: true,
            serverSide: false,
            paging: true,
            searching: true,
            scrollCollapse: true,
            scrollX: true,
            ordering: true,
            ajax: function (requestData, callback, settings) {
                self.loadGroupsForDataTable(callback);
            },
            columnDefs: self.defineColumnDefs()
        }));

        // Bind events to the DataTable
        emailGroupsTable.on('click', 'td button.manage-users-btn', function (event) {
            event.stopPropagation();
            const rowData = emailGroupsTable.row(event.target.closest('tr')).data();
            self.showManageUsersModal(rowData);
        });

        emailGroupsTable.on('click', 'td button.delete-group-btn', function (event) {
            event.stopPropagation();
            const rowData = emailGroupsTable.row(event.target.closest('tr')).data();
            // Check for both lowercase and uppercase
            const isDynamic = rowData.type === 'dynamic' || rowData.type === 'Dynamic';
            if (isDynamic) {
                self.deleteGroup(rowData.id);
            }
        });
    },

    defineColumnDefs: function () {
        return [
            {
                title: "Group Name",
                name: 'name',
                data: 'name'
            },
            {
                title: "Description",
                name: 'description',
                data: 'description',
                defaultContent: '<span class="text-muted">No description</span>'
            },
            {
                title: "Type",
                name: 'type',
                data: 'type',
                render: function (data, type, row) {
                    // Check for lowercase 'static' as that's what the API returns
                    const isStatic = row.type === 'static' || row.type === 'Static';
                    return isStatic ?
                        '<span class="badge bg-secondary">Static</span>' :
                        '<span class="badge bg-primary">Dynamic</span>';
                }
            },
            {
                title: "Actions",
                name: 'actions',
                data: null,
                orderable: false,
                render: function (data, type, row) {
                    // Check for lowercase 'static' as that's what the API returns
                    const isStatic = row.type === 'static' || row.type === 'Static';
                    const $buttonWrapper = $('<div>').addClass('d-flex flex-nowrap gap-1');

                    // Manage Users button (always shown) - using pencil icon
                    const $manageUsersButton = $('<button>')
                        .addClass('btn btn-sm manage-users-btn px-0 float-end')
                        .attr({
                            'aria-label': 'Edit Group',
                            'title': 'Edit Group'
                        }).append($('<i>').addClass('fl fl-edit'));

                    $buttonWrapper.append($manageUsersButton);
                    
                    // Delete button (only shown for Dynamic groups)
                    if (!isStatic) {
                        const $deleteButton = $('<button>')
                            .addClass('btn btn-sm delete-group-btn px-0 float-end')
                            .attr({
                                'aria-label': 'Delete',
                                'title': 'Delete'
                            }).append($('<i>').addClass('fl fl-delete'));
                        
                        $buttonWrapper.append($deleteButton);
                    }

                    return $buttonWrapper.prop('outerHTML');
                }
            }
        ];
    },

    bindEvents: function () {
        const self = this;

        // Remove any existing handlers first
        $('#CreateNewEmailGroup').off('click');

        $('#CreateNewEmailGroup').on('click', function () {
            self.showCreateGroupModal();
        });
    },

    loadGroupsForDataTable: function (callback) {

        unity.notifications.emailGroups.emailGroups.getList().then(function (result) {
            callback({
                recordsTotal: result.length,
                recordsFiltered: result.length,
                data: result
            });
        }).catch(function (error) {
            callback({
                recordsTotal: 0,
                recordsFiltered: 0,
                data: []
            });
        });
    },


    showCreateGroupModal: function () {
        const self = this;
        const selectedUsers = []; // Cache for selected users

        const modalHtml = `
            <div class="modal fade" id="createGroupModal" tabindex="-1">
                <div class="modal-dialog modal-xl">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title mx-5">Add New Group</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <form id="createGroupForm">
                                <div class="mb-3 mx-5">
                                    <label for="groupName" class="form-label">Group Name</label>
                                    <input type="text" class="form-control" id="groupName" required>
                                </div> 
                                <div class="mb-3 mx-5">
                                    <label for="groupDescription" class="form-label">Description</label>
                                    <textarea class="form-control" id="groupDescription" rows="3"></textarea>
                                </div>
                                <hr>
                                <div class="mb-3">
                                    <h6 class="mb-3"><b> Add Member </b></h6>
                                    <label for="createGroupUserSearch" class="form-label mx-5">User Look Up</label>
                                    <div class="dropdown mx-5">
                                        <input type="text" class="form-control dropdown-toggle" id="createGroupUserSearch" 
                                            data-bs-toggle="dropdown" aria-expanded="false" 
                                            placeholder="Start typing user name..." autocomplete="off">
                                        <ul class="dropdown-menu w-100" id="createGroupUserDropdown" style="max-height: 300px; overflow-y: auto;">
                                            <li class="px-3 py-2 text-muted">Start typing to search users...</li>
                                        </ul>
                                    </div>
                                    <button type="button" class="btn btn-add-user mt-2 float-end mx-5" id="createAddUserBtn" disabled>
                                        <i class="fa fa-check me-1"></i>ADD USER
                                    </button>
                                </div>
                                <hr style="margin-top: 60px;">
                                <div>
                                    <h6 class="my-3"><b> Group Members </b></h6>
                                    <div class="table-responsive mt-2 mx-5">
                                        <table class="table table-hover" id="createGroupUsersTable">
                                            <thead>
                                                <tr>
                                                    <th>First Name</th>
                                                    <th>Last Name</th>
                                                    <th>Display Name</th>
                                                    <th></th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                <tr>
                                                    <td colspan="4" class="text-center text-muted">No users selected</td>
                                                </tr>
                                            </tbody>
                                        </table>
                                    </div>
                                </div>
                            </form>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-primary" id="saveNewGroup">Save</button>
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        $('body').append(modalHtml);
        const modal = new bootstrap.Modal(document.getElementById('createGroupModal'));

        // Initialize DataTable for selected users
        let createGroupUsersTable = null;

        // Wait for modal to be fully shown before initializing DataTable
        $('#createGroupModal').on('shown.bs.modal', function () {
            createGroupUsersTable = $('#createGroupUsersTable').DataTable(abp.libs.datatables.normalizeConfiguration(
                self.utils.getStandardDataTableConfig([])
            ));

            // Force columns to adjust
            createGroupUsersTable.columns.adjust().draw();
        });

        modal.show();

        // Initialize user search with shared utility
        self.utils.setupUserSearchDropdown(
            'createGroupUserSearch',
            'createGroupUserDropdown',
            'createAddUserBtn',
            function(selectedUser) {
                // Add to selected users if not already there
                if (!selectedUsers.find(u => u.userId === selectedUser.userId)) {
                    const newUser = {
                        userId: selectedUser.userId,
                        userName: selectedUser.userName,
                        userEmail: selectedUser.userEmail,
                        firstName: selectedUser.firstName || '',
                        lastName: selectedUser.lastName || '',
                        isNew: true
                    };
                    selectedUsers.push(newUser);

                    // Update DataTable
                    if (createGroupUsersTable) {
                        createGroupUsersTable.row.add(newUser).draw();
                    }
                }
            }
        );

        // Override the default displayFilteredUsers for create modal to exclude selected users
        const originalDisplayFilteredUsers = self.utils.displayFilteredUsers.bind(self.utils);
        self.utils.displayFilteredUsers = function(dropdownId, filteredUsers, excludeUserIds = []) {
            if (dropdownId === 'createGroupUserDropdown') {
                const selectedUserIds = selectedUsers.map(u => u.userId);
                excludeUserIds = [...excludeUserIds, ...selectedUserIds];
            }
            return originalDisplayFilteredUsers(dropdownId, filteredUsers, excludeUserIds);
        };

        // Handle removing user from selected list
        $(document).on('click', '#createGroupUsersTable .remove-selected-user', function (e) {
            e.preventDefault();
            const userId = $(this).data('user-id');
            const rowElement = $(this).closest('tr');

            // Remove from array
            const index = selectedUsers.findIndex(u => u.userId === userId);
            if (index > -1) {
                selectedUsers.splice(index, 1);

                // Remove from DataTable
                if (createGroupUsersTable) {
                    createGroupUsersTable.row(rowElement).remove().draw();
                }
            }
        });

        $('#saveNewGroup').on('click', function () {
            const name = $('#groupName').val();
            const description = $('#groupDescription').val();

            if (!name) {
                abp.notify.error('Group name is required');
                return;
            }

            const dto = {
                name: name,
                description: description,
                type: 'Dynamic'
            };

            unity.notifications.emailGroups.emailGroups.create(dto).then(result => {
                if (!(result?.id && selectedUsers?.length)) {
                    abp.notify.success('Email group created successfully');
                    modal.hide();
                    emailGroupsTable?.ajax.reload();
                    return;
                }

                const ops = selectedUsers.map(u =>
                    unity.notifications.emailGroups.emailGroupUsers.insert({ userId: u.userId, groupId: result.id })
                );

                Promise.all(ops).then(() => {
                    abp.notify.success(`Email group created with ${selectedUsers.length} users`);
                    modal.hide();
                    emailGroupsTable?.ajax.reload();
                });
                }).catch(err => {
                console.error('Failed to create email group:', err);
                abp.notify.error('Failed to create email group');
            });
        });

        $('#createGroupModal').on('hidden.bs.modal', function () {
            // Restore original displayFilteredUsers function
            self.utils.displayFilteredUsers = originalDisplayFilteredUsers;
            
            // Clean up DataTable
            if (createGroupUsersTable) {
                createGroupUsersTable.destroy();
            }
            // Clean up event handlers
            $(document).off('click', '#createGroupUserDropdown .dropdown-user-item');
            $(document).off('click', '#createGroupUsersTable .remove-selected-user');
            $(this).remove();
        });
    },

    deleteGroup: function (groupId) {
        abp.message.confirm(
            'Are you sure you want to delete this email group? This action cannot be undone.',
            'Delete Email Group',
            function (isConfirmed) {
                if (isConfirmed) {
                    unity.notifications.emailGroups.emailGroups.delete(groupId).then(function () {
                        abp.notify.success('Email group deleted successfully');
                        if (emailGroupsTable) {
                            emailGroupsTable.ajax.reload();
                        }
                    }).catch(function (error) {
                        console.error('Failed to delete email group:', error);
                        abp.notify.error('Failed to delete email group');
                    });
                }
            }
        );
    },

    showManageUsersModal: function (group) {
        const self = this;
        const isDynamic = group.type === 'dynamic' || group.type === 'Dynamic';

        // Track changes locally
        let pendingUserAdditions = [];
        let pendingUserRemovals = [];

        const modalHtml = `
            <div class="modal fade" id="manageUsersModal" tabindex="-1">
                <div class="modal-dialog modal-xl">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title mx-5">Edit Group</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <div class="mb-3 mx-5">
                                <label for="editGroupName" class="form-label">Group Name</label>
                                <input type="text" class="form-control" id="editGroupName" value="${group.name}" ${!isDynamic ? 'disabled' : ''} required>
                            </div>
                            <div class="mb-3 mx-5">
                                <label for="editGroupDescription" class="form-label">Description</label>
                                <textarea class="form-control" id="editGroupDescription" rows="3">${group.description || ''}</textarea>
                            </div>
                            <hr>
                            <div class="mb-3">
                                <h6 class="mb-3"><b> Add Member </b></h6>
                                <label class="form-label mx-5">User Look Up</label>
                                <div class="dropdown mx-5">
                                    <input type="text" class="form-control dropdown-toggle" id="userSearchInput" 
                                        data-bs-toggle="dropdown" aria-expanded="false" 
                                        placeholder="Start typing user name..." autocomplete="off">
                                    <ul class="dropdown-menu w-100" id="userDropdownMenu" style="max-height: 300px; overflow-y: auto;">
                                        <li class="px-3 py-2 text-muted">Start typing to search users...</li>
                                    </ul>
                                </div>
                                <button type="button" class="btn btn-add-user mt-2 float-end mx-5" id="manageAddUserBtn" disabled>
                                    <i class="fa fa-check me-1"></i>ADD USER
                                </button>
                            </div>
                            <hr style="margin-top: 60px;">
                            <div>
                                <h6 class="my-3"><b> Group Members </b></h6>
                                <div class="table-responsive mt-2 mx-5">
                                    <table class="table table-hover" id="groupUsersTable">
                                        <thead>
                                            <tr>
                                                <th>First Name</th>
                                                <th>Last Name</th>
                                                <th>Display Name</th>
                                                <th style="width: 10px;"></th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            <tr>
                                                <td colspan="4" class="text-center text-muted">Loading users...</td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </div>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-primary" id="saveGroupChanges">Save</button>
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        $('body').append(modalHtml);
        const modal = new bootstrap.Modal(document.getElementById('manageUsersModal'));
        modal.show();

        // Initialize DataTable for group users
        let groupUsersTable = null;

        // Function to add user to local state only
        const addUserLocally = function (userInfo) {
            // Check if user is already in the table
            const existingUsers = groupUsersTable.rows().data().toArray();
            if (existingUsers.some(u => u.userId === userInfo.userId)) {
                abp.notify.warn('User is already in the group');
                return;
            }

            // Check if this user was previously removed (cancel out the removal)
            const removalIndex = pendingUserRemovals.findIndex(u => u.userId === userInfo.userId);
            if (removalIndex > -1) {
                pendingUserRemovals.splice(removalIndex, 1);
            } else if (!pendingUserAdditions.some(u => u.userId === userInfo.userId)) {
                pendingUserAdditions.push(userInfo);
            }

            // Add to DataTable for visual feedback
            const newRow = {
                id: 'temp_' + userInfo.userId, // Temporary ID for new users
                userId: userInfo.userId,
                groupId: group.id,
                userName: userInfo.userName,
                email: userInfo.userEmail || userInfo.email || '',
                firstName: userInfo.firstName || '',
                lastName: userInfo.lastName || '',
                isNew: true // Flag to identify newly added users
            };
            groupUsersTable.row.add(newRow).draw();
        };

        // Function to remove user from local state only
        const removeUserLocally = function (rowElement, rowData) {
            const userId = rowData.userId;

            // Check if this is a newly added user (not yet saved)
            const additionIndex = pendingUserAdditions.findIndex(u => u.userId === userId);
            if (additionIndex > -1) {
                // Remove from pending additions
                pendingUserAdditions.splice(additionIndex, 1);
            } else if (!pendingUserRemovals.some(u => u.userId === userId)) {
                // Add to pending removals if not already there
                pendingUserRemovals.push(rowData);
            }

            // Remove from DataTable for visual feedback
            groupUsersTable.row(rowElement).remove().draw();
        };

        $('#manageUsersModal').one('shown.bs.modal', () => {
            groupUsersTable = $('#groupUsersTable').DataTable(
                abp.libs.datatables.normalizeConfiguration(self.utils.getStandardDataTableConfig([]))
            );
            self.loadGroupUsersForTable(group.id, groupUsersTable);
        });

        // Initialize user search with shared utility and custom filtering for existing group users
        self.utils.setupUserSearchDropdown(
            'userSearchInput',
            'userDropdownMenu',
            'manageAddUserBtn',
            function(selectedUser) {
                addUserLocally(selectedUser);
            }
        );

        // Override the default displayFilteredUsers for manage modal to exclude current group users
        const originalDisplayFilteredUsers = self.utils.displayFilteredUsers.bind(self.utils);
        self.utils.displayFilteredUsers = function(dropdownId, filteredUsers, excludeUserIds = []) {
            if (dropdownId === 'userDropdownMenu') {
                // Get current group users from DataTable and exclude them
                const currentUsers = groupUsersTable ? groupUsersTable.rows().data().toArray() : [];
                const currentUserIds = currentUsers.map(u => u.userId);
                excludeUserIds = [...excludeUserIds, ...currentUserIds];
            }
            return originalDisplayFilteredUsers(dropdownId, filteredUsers, excludeUserIds);
        };

        // Also show dropdown on focus
        $('#userSearchInput').on('focus', function () {
            if ($(this).val().length > 0) {
                $('#userDropdownMenu').addClass('show');
                $(this).attr('aria-expanded', 'true');
            }
        });

        // Handle remove user button click - removes locally only
        $('#manageUsersModal').on('click', '.remove-user-btn, .remove-selected-user', function (e) {
            e.preventDefault();
            e.stopPropagation();
            const rowElement = $(this).closest('tr');
            const rowData = groupUsersTable.row(rowElement).data();
            if (rowData) {
                removeUserLocally(rowElement, rowData);
            }
        });

        // Save all changes (group info and user changes)
        $('#saveGroupChanges').on('click', function () {
            const name = $('#editGroupName').val();
            const description = $('#editGroupDescription').val();

            if (!name) {
                abp.notify.error('Group name is required');
                return;
            }

            // Disable save button to prevent double-clicking
            $(this).prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

            // Create array of promises for all operations
            const promises = [];

            // Update group info if changed
            const groupInfoChanged = (isDynamic && name !== group.name) || description !== (group.description || '');
            if (groupInfoChanged) {
                const dto = {
                    id: group.id,
                    name: isDynamic ? name : group.name,
                    description: description,
                    type: group.type
                };
                promises.push(
                    unity.notifications.emailGroups.emailGroups.update(dto)
                        .catch(error => {
                            console.error('Failed to update group:', error);
                            throw new Error('Failed to update group information');
                        })
                );
            }

            // Process user removals
            pendingUserRemovals.forEach(user => {
                if (user.id && user.id !== 'temp_' + user.userId) {
                    promises.push(
                        unity.notifications.emailGroups.emailGroupUsers.deleteUser(user.id)
                            .catch(error => {
                                console.error(`Failed to remove user ${user.userName}:`, error);
                                throw new Error(`Failed to remove user ${user.userName}`);
                            })
                    );
                }
            });

            // Process user additions
            pendingUserAdditions.forEach(user => {
                const dto = {
                    userId: user.userId,
                    groupId: group.id
                };
                promises.push(
                    unity.notifications.emailGroups.emailGroupUsers.insert(dto)
                        .catch(error => {
                            console.error(`Failed to add user ${user.userName}:`, error);
                            throw new Error(`Failed to add user ${user.userName}`);
                        })
                );
            });

            // Execute all promises
            if (promises.length > 0) {
                Promise.all(promises)
                    .then(() => {
                        abp.notify.success('All changes saved successfully');

                        // Reload the main table
                        if (emailGroupsTable) {
                            emailGroupsTable.ajax.reload();
                        }

                        // Close the modal
                        const modal = bootstrap.Modal.getInstance(document.getElementById('manageUsersModal'));
                        modal.hide();
                    })
                    .catch(error => {
                        abp.notify.error(error.message || 'Failed to save some changes');
                        // Re-enable save button
                        $('#saveGroupChanges').prop('disabled', false).html('Save');
                    });
            } else {
                // No changes to save
                abp.notify.info('No changes to save');
                $('#saveGroupChanges').prop('disabled', false).html('Save');
            }
        });

        // Handle modal close/cancel
        $('#manageUsersModal').on('hidden.bs.modal', function () {

            // Restore original displayFilteredUsers function
            self.utils.displayFilteredUsers = originalDisplayFilteredUsers;

            if (groupUsersTable) {
                groupUsersTable.destroy();
            }
            // Clean up event handlers
            $(document).off('click', '#userDropdownMenu .dropdown-user-item');
            $(this).remove();
        });
    },


    loadAllUsers: function (callback) {
        // Load all users for the dropdown
        $.ajax({
            url: '/api/identity/users',
            method: 'GET',
            data: {
                maxResultCount: 1000,
                skipCount: 0
            },
            headers: {
                'RequestVerificationToken': abp.security.antiForgery.getToken()
            },
            success: function (response) {
                const users = response.items || response || [];
                callback(users);
            },
            error: function (xhr, status, error) {
                console.error('Failed to load users:', error);
                callback([]);
            }
        });
    },


    loadGroupUsersForTable: function (groupId, dataTable) {

        unity.notifications.emailGroups.emailGroupUsers.getEmailGroupUsersByGroupId(groupId).then(function (groupUsers) {

            if (!groupUsers || groupUsers.length === 0) {
                dataTable.clear().draw();
                return;
            }

            // Fetch user details for each group user
            const userPromises = groupUsers.map(gu =>
                $.ajax({
                    url: `/api/identity/users/${gu.userId}`,
                    method: 'GET'
                }).then(function (user) {
                    return {
                        id: gu.id,
                        userId: gu.userId,
                        groupId: gu.groupId,
                        userName: user.userName || user.name,
                        email: user.email,
                        firstName: user.name ? user.name.split(' ')[0] : '',
                        lastName: user.surname || ''
                    };
                }).catch(function () {
                    return {
                        id: gu.id,
                        userId: gu.userId,
                        groupId: gu.groupId,
                        userName: 'Unknown User',
                        email: '',
                        firstName: '',
                        lastName: ''
                    };
                })
            );

            Promise.all(userPromises).then(function (usersWithDetails) {
                // Create unique list based on userId to prevent duplicates
                const uniqueUsers = [];
                const seenUserIds = new Set();

                usersWithDetails.forEach(user => {
                    if (!seenUserIds.has(user.userId)) {
                        seenUserIds.add(user.userId);
                        uniqueUsers.push(user);
                    }
                });

                // Update DataTable with users
                dataTable.clear();
                dataTable.rows.add(uniqueUsers);
                dataTable.draw();
            });
        }).catch(function (error) {
            console.error('Failed to load group users:', error);
            abp.notify.error('Failed to load group users');
        });
    },

};

$(document).on('shown.bs.tab', '#nav-internal-email-group-tab', () => {
    emailGroupsManager.init();
});