(function ($) {
    $(function () {
        let emailGroupsTable = null;
        let userCanUpdate = true; // You may want to check permissions here
        let userCanDelete = true; // You may want to check permissions here
        
        const emailGroupsManager = {
            groups: [],
            initialized: false,
            
            init: function() {
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

            initializeDataTable: function() {
                const self = this;
                
                emailGroupsTable = $('#EmailGroupsTable').DataTable(abp.libs.datatables.normalizeConfiguration({
                    processing: true,
                    serverSide: false,
                    paging: true,
                    searching: true,
                    scrollCollapse: true,
                    scrollX: true,
                    ordering: true,
                    ajax: function(requestData, callback, settings) {
                        self.loadGroupsForDataTable(callback);
                    },
                    columnDefs: self.defineColumnDefs()
                }));
                
                // Bind events to the DataTable
                emailGroupsTable.on('click', 'td button.manage-users-btn', function(event) {
                    event.stopPropagation();
                    const rowData = emailGroupsTable.row(event.target.closest('tr')).data();
                    self.showManageUsersModal(rowData);
                });
                
                emailGroupsTable.on('click', 'td button.edit-group-btn', function(event) {
                    event.stopPropagation();
                    const rowData = emailGroupsTable.row(event.target.closest('tr')).data();
                    // Check for both lowercase and uppercase
                    const isDynamic = rowData.type === 'dynamic' || rowData.type === 'Dynamic';
                    if (isDynamic) {
                        self.showEditGroupModal(rowData);
                    }
                });
                
                emailGroupsTable.on('click', 'td button.delete-group-btn', function(event) {
                    event.stopPropagation();
                    const rowData = emailGroupsTable.row(event.target.closest('tr')).data();
                    // Check for both lowercase and uppercase
                    const isDynamic = rowData.type === 'dynamic' || rowData.type === 'Dynamic';
                    if (isDynamic) {
                        self.deleteGroup(rowData.id);
                    }
                });
            },
            
            defineColumnDefs: function() {
                const self = this;
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
                        render: function(data, type, row) {
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
                        render: function(data, type, row) {
                            // Check for lowercase 'static' as that's what the API returns
                            const isStatic = row.type === 'static' || row.type === 'Static';
                            const $buttonWrapper = $('<div>').addClass('d-flex flex-nowrap gap-1');
                            
                            // Manage Users button (always shown) - using outline version
                            const $manageUsersButton = $('<button>')
                                .addClass('btn btn-sm manage-users-btn px-0 float-end')
                                .attr({
                                    'aria-label': 'Manage Users',
                                    'title': 'Manage Users'
                                }).append($('<i>').addClass('fa fa-user-o'));
                            
                            // Edit button (only for Dynamic groups)
                            const $editButton = $('<button>')
                                .addClass('btn btn-sm edit-group-btn px-0 float-end')
                                .attr({
                                    'aria-label': 'Edit',
                                    'title': 'Edit',
                                    'disabled': isStatic || !userCanUpdate
                                }).append($('<i>').addClass('fl fl-edit'));
                            
                            // Delete button (only for Dynamic groups)
                            const $deleteButton = $('<button>')
                                .addClass('btn btn-sm delete-group-btn px-0 float-end')
                                .attr({
                                    'aria-label': 'Delete',
                                    'title': 'Delete',
                                    'disabled': isStatic || !userCanDelete
                                }).append($('<i>').addClass('fl fl-delete'));
                            
                            $buttonWrapper.append($manageUsersButton);
                            $buttonWrapper.append($editButton);
                            $buttonWrapper.append($deleteButton);
                            
                            return $buttonWrapper.prop('outerHTML');
                        }
                    }
                ];
            },
            
            bindEvents: function() {
                const self = this;
                
                // Remove any existing handlers first
                $('#CreateNewEmailGroup').off('click');
                
                $('#CreateNewEmailGroup').on('click', function() {
                    self.showCreateGroupModal();
                });
            },

            loadGroupsForDataTable: function(callback) {
                const self = this;
                
                unity.notifications.emailGroups.emailGroups.getList().then(function(result) {
                    console.log('Email groups loaded:', result);
                    // Debug: Log the first group to see the actual structure
                    if (result && result.length > 0) {
                        console.log('First group structure:', result[0]);
                        console.log('Type value:', result[0].type);
                    }
                    self.groups = result;
                    callback({
                        recordsTotal: result.length,
                        recordsFiltered: result.length,
                        data: result
                    });
                }).catch(function(error) {
                    console.error('Failed to load email groups:', error);
                    abp.notify.error('Failed to load email groups');
                    callback({
                        recordsTotal: 0,
                        recordsFiltered: 0,
                        data: []
                    });
                });
            },


            showCreateGroupModal: function() {
                const self = this;
                const selectedUsers = []; // Cache for selected users
                
                const modalHtml = `
                    <div class="modal fade" id="createGroupModal" tabindex="-1">
                        <div class="modal-dialog modal-xl">
                            <div class="modal-content">
                                <div class="modal-header">
                                    <h5 class="modal-title">Create New Email Group</h5>
                                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                                </div>
                                <div class="modal-body">
                                    <form id="createGroupForm">
                                        <div class="mb-3">
                                            <label for="groupName" class="form-label">Group Name *</label>
                                            <input type="text" class="form-control" id="groupName" required>
                                        </div>
                                        <div class="mb-3">
                                            <label for="groupDescription" class="form-label">Description</label>
                                            <textarea class="form-control" id="groupDescription" rows="3"></textarea>
                                        </div>
                                        <hr>
                                        <div class="mb-3">
                                            <label class="form-label">User Look Up</label>
                                            <div class="dropdown">
                                                <input type="text" class="form-control dropdown-toggle" id="createGroupUserSearch" 
                                                    data-bs-toggle="dropdown" aria-expanded="false" 
                                                    placeholder="Start typing user name..." autocomplete="off">
                                                <ul class="dropdown-menu w-100" id="createGroupUserDropdown" style="max-height: 300px; overflow-y: auto;">
                                                    <li class="px-3 py-2 text-muted">Start typing to search users...</li>
                                                </ul>
                                            </div>
                                            <button type="button" class="btn btn-add-user mt-2 float-end" id="createAddUserBtn" disabled>
                                                <i class="fa fa-check me-1"></i>ADD USER
                                            </button>
                                        </div>
                                        <div>
                                            <label class="form-label">Selected Users</label>
                                            <div class="table-responsive mt-2">
                                                <table class="table table-hover" id="createGroupUsersTable">
                                                    <thead>
                                                        <tr>
                                                            <th>User Name</th>
                                                            <th>Email</th>
                                                            <th style="width: 10px;"></th>
                                                        </tr>
                                                    </thead>
                                                    <tbody>
                                                        <tr>
                                                            <td colspan="3" class="text-center text-muted">No users selected</td>
                                                        </tr>
                                                    </tbody>
                                                </table>
                                            </div>
                                        </div>
                                    </form>
                                </div>
                                <div class="modal-footer">
                                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                                    <button type="button" class="btn btn-primary" id="saveNewGroup">Create Group</button>
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
                $('#createGroupModal').on('shown.bs.modal', function() {
                    createGroupUsersTable = $('#createGroupUsersTable').DataTable(abp.libs.datatables.normalizeConfiguration({
                        processing: false,
                        serverSide: false,
                        paging: false,
                        searching: false,
                        info: false,
                        ordering: true,
                        scrollCollapse: false,
                        scrollX: false,
                        autoWidth: false,
                        data: [],
                        columnDefs: [
                            {
                                title: "User Name",
                                name: 'userName',
                                data: 'userName',
                                width: "45%"
                            },
                            {
                                title: "Email",
                                name: 'email',
                                data: 'userEmail',
                                width: "45%"
                            },
                            {
                                title: "",
                                name: 'actions',
                                data: null,
                                orderable: false,
                                width: "10%",
                                className: "text-end pe-0",
                                render: function(data, type, row) {
                                    return `<div class="text-end"><button class="btn btn-sm btn-link text-primary p-0 remove-selected-user" style="text-decoration: none; margin-right: 0;" data-user-id="${row.userId}" title="Remove user">
                                        <i class="fa fa-times"></i>
                                    </button></div>`;
                                }
                            }
                        ]
                    }));
                    
                    // Force columns to adjust
                    createGroupUsersTable.columns.adjust().draw();
                });
                
                modal.show();
                
                // Initialize user search for create modal
                let searchTimeout;
                let allUsers = [];
                let selectedCreateUser = null;
                
                // Load all users for the dropdown
                self.loadAllUsers(function(users) {
                    allUsers = users;
                });
                
                // Filter users as user types
                $('#createGroupUserSearch').on('input', function() {
                    clearTimeout(searchTimeout);
                    const searchTerm = $(this).val().toLowerCase();
                    
                    // Show dropdown if not already shown
                    if (!$('#createGroupUserDropdown').hasClass('show')) {
                        $('#createGroupUserDropdown').addClass('show');
                        $(this).attr('aria-expanded', 'true');
                    }
                    
                    searchTimeout = setTimeout(() => {
                        if (searchTerm.length > 0) {
                            self.filterUsersForCreateModal(allUsers, searchTerm, selectedUsers);
                        } else {
                            $('#createGroupUserDropdown').html('<li class="px-3 py-2 text-muted">Start typing to search users...</li>');
                        }
                    }, 300);
                });
                
                // Handle selecting user from dropdown
                $(document).on('click', '#createGroupUserDropdown .dropdown-user-item', function(e) {
                    e.preventDefault();
                    e.stopPropagation();
                    const userId = $(this).data('user-id');
                    const userName = $(this).data('user-name');
                    const userEmail = $(this).data('user-email');
                    
                    // Store selected user
                    selectedCreateUser = { userId, userName, userEmail };
                    
                    // Update search input with selected user
                    $('#createGroupUserSearch').val(userName);
                    
                    // Enable the Add User button
                    $('#createAddUserBtn').prop('disabled', false);
                    
                    // Hide dropdown
                    $('#createGroupUserDropdown').removeClass('show');
                    $('#createGroupUserSearch').attr('aria-expanded', 'false');
                });
                
                // Handle Add User button click
                $('#createAddUserBtn').on('click', function() {
                    if (selectedCreateUser) {
                        // Add to selected users if not already there
                        if (!selectedUsers.find(u => u.userId === selectedCreateUser.userId)) {
                            const newUser = { 
                                userId: selectedCreateUser.userId, 
                                userName: selectedCreateUser.userName, 
                                userEmail: selectedCreateUser.userEmail 
                            };
                            selectedUsers.push(newUser);
                            
                            // Update DataTable
                            if (createGroupUsersTable) {
                                createGroupUsersTable.row.add(newUser).draw();
                            }
                        }
                        
                        // Reset state
                        selectedCreateUser = null;
                        $('#createGroupUserSearch').val('');
                        $('#createAddUserBtn').prop('disabled', true);
                        $('#createGroupUserDropdown').html('<li class="px-3 py-2 text-muted">Start typing to search users...</li>');
                    }
                });
                
                // Clear selection when search input changes
                $('#createGroupUserSearch').on('input', function() {
                    const inputVal = $(this).val();
                    if (!inputVal || (selectedCreateUser && inputVal !== selectedCreateUser.userName)) {
                        selectedCreateUser = null;
                        $('#createAddUserBtn').prop('disabled', true);
                    }
                });
                
                // Handle removing user from selected list
                $(document).on('click', '#createGroupUsersTable .remove-selected-user', function(e) {
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

                $('#saveNewGroup').on('click', function() {
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

                    unity.notifications.emailGroups.emailGroups.create(dto).then(function(result) {
                        // Add selected users to the newly created group
                        if (selectedUsers.length > 0 && result && result.id) {
                            const userPromises = selectedUsers.map(user => 
                                unity.notifications.emailGroups.emailGroupUsers.insert({
                                    userId: user.userId,
                                    groupId: result.id
                                }).catch(error => {
                                    console.error(`Failed to add user ${user.userName} to group:`, error);
                                })
                            );
                            
                            Promise.all(userPromises).then(() => {
                                abp.notify.success(`Email group created with ${selectedUsers.length} users`);
                                modal.hide();
                                if (emailGroupsTable) {
                                    emailGroupsTable.ajax.reload();
                                }
                            });
                        } else {
                            abp.notify.success('Email group created successfully');
                            modal.hide();
                            if (emailGroupsTable) {
                                emailGroupsTable.ajax.reload();
                            }
                        }
                    }).catch(function(error) {
                        console.error('Failed to create email group:', error);
                        abp.notify.error('Failed to create email group');
                    });
                });

                $('#createGroupModal').on('hidden.bs.modal', function() {
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

            showEditGroupModal: function(group) {
                const self = this;
                const modalHtml = `
                    <div class="modal fade" id="editGroupModal" tabindex="-1">
                        <div class="modal-dialog">
                            <div class="modal-content">
                                <div class="modal-header">
                                    <h5 class="modal-title">Edit Email Group</h5>
                                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                                </div>
                                <div class="modal-body">
                                    <form id="editGroupForm">
                                        <div class="mb-3">
                                            <label for="editGroupName" class="form-label">Group Name *</label>
                                            <input type="text" class="form-control" id="editGroupName" value="${group.name}" required>
                                        </div>
                                        <div class="mb-3">
                                            <label for="editGroupDescription" class="form-label">Description</label>
                                            <textarea class="form-control" id="editGroupDescription" rows="3">${group.description || ''}</textarea>
                                        </div>
                                    </form>
                                </div>
                                <div class="modal-footer">
                                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                                    <button type="button" class="btn btn-primary" id="updateGroup">Save Changes</button>
                                </div>
                            </div>
                        </div>
                    </div>
                `;

                $('body').append(modalHtml);
                const modal = new bootstrap.Modal(document.getElementById('editGroupModal'));
                modal.show();

                $('#updateGroup').on('click', function() {
                    const name = $('#editGroupName').val();
                    const description = $('#editGroupDescription').val();

                    if (!name) {
                        abp.notify.error('Group name is required');
                        return;
                    }

                    const dto = {
                        id: group.id,
                        name: name,
                        description: description,
                        type: group.type
                    };

                    unity.notifications.emailGroups.emailGroups.update(dto).then(function(result) {
                        abp.notify.success('Email group updated successfully');
                        modal.hide();
                        if (emailGroupsTable) {
                            emailGroupsTable.ajax.reload();
                        }
                    }).catch(function(error) {
                        console.error('Failed to update email group:', error);
                        abp.notify.error('Failed to update email group');
                    });
                });

                $('#editGroupModal').on('hidden.bs.modal', function() {
                    $(this).remove();
                });
            },

            deleteGroup: function(groupId) {
                const self = this;
                abp.message.confirm(
                    'Are you sure you want to delete this email group? This action cannot be undone.',
                    'Delete Email Group',
                    function(isConfirmed) {
                        if (isConfirmed) {
                            unity.notifications.emailGroups.emailGroups.delete(groupId).then(function() {
                                abp.notify.success('Email group deleted successfully');
                                if (emailGroupsTable) {
                                    emailGroupsTable.ajax.reload();
                                }
                            }).catch(function(error) {
                                console.error('Failed to delete email group:', error);
                                abp.notify.error('Failed to delete email group');
                            });
                        }
                    }
                );
            },

            showManageUsersModal: function(group) {
                const self = this;
                const modalHtml = `
                    <div class="modal fade" id="manageUsersModal" tabindex="-1">
                        <div class="modal-dialog modal-xl">
                            <div class="modal-content">
                                <div class="modal-header">
                                    <h5 class="modal-title">Manage Users - ${group.name}</h5>
                                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                                </div>
                                <div class="modal-body">
                                    <div class="mb-3">
                                        <label class="form-label">User Look Up</label>
                                        <div class="dropdown">
                                            <input type="text" class="form-control dropdown-toggle" id="userSearchInput" 
                                                data-bs-toggle="dropdown" aria-expanded="false" 
                                                placeholder="Start typing user name..." autocomplete="off">
                                            <ul class="dropdown-menu w-100" id="userDropdownMenu" style="max-height: 300px; overflow-y: auto;">
                                                <li class="px-3 py-2 text-muted">Start typing to search users...</li>
                                            </ul>
                                        </div>
                                        <button type="button" class="btn btn-add-user mt-2 float-end" id="manageAddUserBtn" disabled>
                                            <i class="fa fa-check me-1"></i>ADD USER
                                        </button>
                                    </div>
                                    <hr>
                                    <div>
                                        <label class="form-label">Current Group Members</label>
                                        <div class="table-responsive mt-2">
                                            <table class="table table-hover" id="groupUsersTable">
                                                <thead>
                                                    <tr>
                                                        <th>User Name</th>
                                                        <th>Email</th>
                                                        <th style="width: 10px;"></th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                                    <tr>
                                                        <td colspan="3" class="text-center text-muted">Loading users...</td>
                                                    </tr>
                                                </tbody>
                                            </table>
                                        </div>
                                    </div>
                                </div>
                                <div class="modal-footer">
                                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
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
                setTimeout(() => {
                    groupUsersTable = $('#groupUsersTable').DataTable(abp.libs.datatables.normalizeConfiguration({
                        processing: false,
                        serverSide: false,
                        paging: false,
                        searching: false,
                        info: false,
                        ordering: true,
                        scrollCollapse: true,
                        data: [],
                        columnDefs: [
                            {
                                title: "User Name",
                                name: 'userName',
                                data: 'userName'
                            },
                            {
                                title: "Email",
                                name: 'email',
                                data: 'email'
                            },
                            {
                                title: "",
                                name: 'actions',
                                data: null,
                                orderable: false,
                                width: "10px",
                                className: "text-end pe-0",
                                render: function(data, type, row) {
                                    return `<div class="text-end"><button class="btn btn-sm btn-link text-primary p-0 remove-user-btn" style="text-decoration: none; margin-right: 0;" data-group-user-id="${row.id}" title="Remove user">
                                        <i class="fa fa-times"></i>
                                    </button></div>`;
                                }
                            }
                        ]
                    }));
                    
                    // Load users after DataTable is initialized
                    self.loadGroupUsersForTable(group.id, groupUsersTable);
                }, 100);

                // Initialize user search dropdown
                let searchTimeout;
                let allUsers = [];
                let selectedUser = null;
                
                // Load all users initially
                self.loadAllUsers(function(users) {
                    allUsers = users;
                });
                
                // Filter users as user types
                $('#userSearchInput').on('input', function() {
                    clearTimeout(searchTimeout);
                    const searchTerm = $(this).val().toLowerCase();
                    
                    // Show dropdown if not already shown
                    if (!$('#userDropdownMenu').hasClass('show')) {
                        $('#userDropdownMenu').addClass('show');
                        $(this).attr('aria-expanded', 'true');
                    }
                    
                    searchTimeout = setTimeout(() => {
                        if (searchTerm.length > 0) {
                            self.filterAndDisplayUsers(allUsers, searchTerm, group.id);
                        } else {
                            $('#userDropdownMenu').html('<li class="px-3 py-2 text-muted">Start typing to search users...</li>');
                        }
                    }, 300);
                });
                
                // Also show dropdown on focus
                $('#userSearchInput').on('focus', function() {
                    if ($(this).val().length > 0) {
                        $('#userDropdownMenu').addClass('show');
                        $(this).attr('aria-expanded', 'true');
                    }
                });
                
                // Handle selecting user from dropdown
                $(document).on('click', '#userDropdownMenu .dropdown-user-item', function(e) {
                    e.preventDefault();
                    e.stopPropagation();
                    const userId = $(this).data('user-id');
                    const userName = $(this).data('user-name');
                    const userEmail = $(this).data('user-email');
                    
                    // Store selected user
                    selectedUser = { userId, userName, userEmail };
                    
                    // Update search input with selected user
                    $('#userSearchInput').val(userName);
                    
                    // Enable the Add User button
                    $('#manageAddUserBtn').prop('disabled', false);
                    
                    // Hide dropdown
                    $('#userDropdownMenu').removeClass('show');
                    $('#userSearchInput').attr('aria-expanded', 'false');
                });
                
                // Handle Add User button click
                $('#manageAddUserBtn').on('click', function() {
                    if (selectedUser) {
                        console.log('Adding user:', selectedUser);
                        self.addUserToGroup(selectedUser.userId, group.id, groupUsersTable);
                        
                        // Reset state
                        selectedUser = null;
                        $('#userSearchInput').val('');
                        $('#manageAddUserBtn').prop('disabled', true);
                    }
                });
                
                // Clear selection when search input changes
                $('#userSearchInput').on('input', function() {
                    const inputVal = $(this).val();
                    if (!inputVal || (selectedUser && inputVal !== selectedUser.userName)) {
                        selectedUser = null;
                        $('#manageAddUserBtn').prop('disabled', true);
                    }
                });

                $('#manageUsersModal').on('click', '.remove-user-btn', function() {
                    const groupUserId = $(this).data('group-user-id');
                    self.removeUserFromGroup(groupUserId, group.id, groupUsersTable);
                });

                $('#manageUsersModal').on('hidden.bs.modal', function() {
                    if (groupUsersTable) {
                        groupUsersTable.destroy();
                    }
                    // Clean up event handlers
                    $(document).off('click', '#userDropdownMenu .dropdown-user-item');
                    $(this).remove();
                });
            },

            filterUsersForCreateModal: function(allUsers, searchTerm, selectedUsers) {
                const dropdownMenu = $('#createGroupUserDropdown');
                
                // Filter users by search term
                const filteredUsers = allUsers.filter(user => {
                    const userName = (user.userName || user.name || '').toLowerCase();
                    const email = (user.email || '').toLowerCase();
                    return userName.includes(searchTerm) || email.includes(searchTerm);
                });
                
                // Exclude already selected users
                const selectedUserIds = selectedUsers.map(u => u.userId);
                const availableUsers = filteredUsers.filter(user => !selectedUserIds.includes(user.id));
                
                if (availableUsers.length === 0) {
                    dropdownMenu.html('<li class="px-3 py-2 text-muted">No matching users found</li>');
                    return;
                }
                
                // Create dropdown items
                const itemsHtml = availableUsers.slice(0, 20).map(user => `
                    <li>
                        <a class="dropdown-item dropdown-user-item" href="#" 
                           data-user-id="${user.id}" 
                           data-user-name="${user.userName || user.name || 'Unknown'}"
                           data-user-email="${user.email || ''}">
                            <div>
                                <strong>${user.userName || user.name || 'Unknown'}</strong>
                                <small class="text-muted d-block">${user.email || ''}</small>
                            </div>
                        </a>
                    </li>
                `).join('');
                
                dropdownMenu.html(itemsHtml);
            },
            
            loadAllUsers: function(callback) {
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
                    success: function(response) {
                        const users = response.items || response || [];
                        callback(users);
                    },
                    error: function(xhr, status, error) {
                        console.error('Failed to load users:', error);
                        callback([]);
                    }
                });
            },
            
            filterAndDisplayUsers: function(allUsers, searchTerm, groupId) {
                const self = this;
                const dropdownMenu = $('#userDropdownMenu');
                
                // Filter users by search term
                const filteredUsers = allUsers.filter(user => {
                    const userName = (user.userName || user.name || '').toLowerCase();
                    const email = (user.email || '').toLowerCase();
                    return userName.includes(searchTerm) || email.includes(searchTerm);
                });
                
                // Get current group users to exclude them
                unity.notifications.emailGroups.emailGroupUsers.getEmailGroupUsersByGroupId(groupId).then(function(groupUsers) {
                    const groupUserIds = (groupUsers || []).map(gu => gu.userId);
                    const availableUsers = filteredUsers.filter(user => !groupUserIds.includes(user.id));
                    
                    if (availableUsers.length === 0) {
                        dropdownMenu.html('<li class="px-3 py-2 text-muted">No matching users found</li>');
                        return;
                    }
                    
                    // Create dropdown items
                    const itemsHtml = availableUsers.slice(0, 20).map(user => `
                        <li>
                            <a class="dropdown-item dropdown-user-item" href="#" 
                               data-user-id="${user.id}" 
                               data-user-name="${user.userName || user.name || 'Unknown'}"
                               data-user-email="${user.email || ''}">
                                <div>
                                    <strong>${user.userName || user.name || 'Unknown'}</strong>
                                    <small class="text-muted d-block">${user.email || ''}</small>
                                </div>
                            </a>
                        </li>
                    `).join('');
                    
                    dropdownMenu.html(itemsHtml);
                }).catch(function() {
                    // If we can't get group users, show all filtered users
                    if (filteredUsers.length === 0) {
                        dropdownMenu.html('<li class="px-3 py-2 text-muted">No matching users found</li>');
                        return;
                    }
                    
                    const itemsHtml = filteredUsers.slice(0, 20).map(user => `
                        <li>
                            <a class="dropdown-item dropdown-user-item" href="#" 
                               data-user-id="${user.id}" 
                               data-user-name="${user.userName || user.name || 'Unknown'}"
                               data-user-email="${user.email || ''}">
                                <div>
                                    <strong>${user.userName || user.name || 'Unknown'}</strong>
                                    <small class="text-muted d-block">${user.email || ''}</small>
                                </div>
                            </a>
                        </li>
                    `).join('');
                    
                    dropdownMenu.html(itemsHtml);
                });
            },
            
            loadGroupUsersForTable: function(groupId, dataTable) {
                const self = this;
                
                console.log('Loading users for group:', groupId);
                
                // Always fetch fresh data from backend - no caching
                unity.notifications.emailGroups.emailGroupUsers.getEmailGroupUsersByGroupId(groupId).then(function(groupUsers) {
                    console.log('Fresh group users loaded from backend:', groupUsers);
                    
                    if (!groupUsers || groupUsers.length === 0) {
                        dataTable.clear().draw();
                        return;
                    }
                    
                    // Fetch user details for each group user
                    const userPromises = groupUsers.map(gu => 
                        $.ajax({
                            url: `/api/identity/users/${gu.userId}`,
                            method: 'GET'
                        }).then(function(user) {
                            return {
                                id: gu.id,
                                userId: gu.userId,
                                groupId: gu.groupId,
                                userName: user.userName || user.name,
                                email: user.email
                            };
                        }).catch(function() {
                            return {
                                id: gu.id,
                                userId: gu.userId,
                                groupId: gu.groupId,
                                userName: 'Unknown User',
                                email: ''
                            };
                        })
                    );
                    
                    Promise.all(userPromises).then(function(usersWithDetails) {
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
                }).catch(function(error) {
                    console.error('Failed to load group users:', error);
                    abp.notify.error('Failed to load group users');
                });
            },


            addUserToGroup: function(userId, groupId, dataTable) {
                const self = this;
                const dto = {
                    userId: userId,
                    groupId: groupId
                };

                unity.notifications.emailGroups.emailGroupUsers.insert(dto).then(function() {
                    abp.notify.success('User added to group');
                    if (dataTable) {
                        self.loadGroupUsersForTable(groupId, dataTable);
                    }
                    // Clear the search input and dropdown
                    $('#userSearchInput').val('');
                    $('#userDropdownMenu').html('<li class="px-3 py-2 text-muted">Start typing to search users...</li>');
                }).catch(function(error) {
                    console.error('Failed to add user to group:', error);
                    abp.notify.error('Failed to add user to group');
                });
            },

            removeUserFromGroup: function(groupUserId, groupId, dataTable) {
                const self = this;
                
                unity.notifications.emailGroups.emailGroupUsers.deleteUser(groupUserId).then(function() {
                    abp.notify.success('User removed from group');
                    if (dataTable) {
                        self.loadGroupUsersForTable(groupId, dataTable);
                    }
                }).catch(function(error) {
                    console.error('Failed to remove user from group:', error);
                    abp.notify.error('Failed to remove user from group');
                });
            }
        };

        // Initialize only when the Internal Email Group tab is shown
        $('#nav-internal-email-group-tab').on('shown.bs.tab', function() {
            emailGroupsManager.init();
        });
    });
})(jQuery);