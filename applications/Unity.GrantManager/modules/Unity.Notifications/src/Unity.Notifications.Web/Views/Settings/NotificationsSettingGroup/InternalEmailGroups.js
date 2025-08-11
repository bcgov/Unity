(function ($) {
    $(function () {
        const emailGroupsManager = {
            groups: [],
            initialized: false,
            
            init: function() {
                if (this.initialized) {
                    this.loadGroups(); // Only reload data, don't rebind events
                    return;
                }
                this.loadGroups();
                this.bindEvents();
                this.initialized = true;
            },

            bindEvents: function() {
                const self = this;
                
                // Remove any existing handlers first
                $('#CreateNewEmailGroup').off('click');
                $('#emailGroupsContainer').off('click');
                
                $('#CreateNewEmailGroup').on('click', function() {
                    self.showCreateGroupModal();
                });

                // Use container-specific event delegation instead of document
                $('#emailGroupsContainer').on('click', '.edit-group-btn', function() {
                    const groupId = $(this).data('group-id');
                    const group = self.groups.find(g => g.id === groupId);
                    if (group && group.type === 'Dynamic') {
                        self.showEditGroupModal(group);
                    }
                });

                $('#emailGroupsContainer').on('click', '.delete-group-btn', function() {
                    const groupId = $(this).data('group-id');
                    const group = self.groups.find(g => g.id === groupId);
                    if (group && group.type === 'Dynamic') {
                        self.deleteGroup(groupId);
                    }
                });

                $('#emailGroupsContainer').on('click', '.manage-users-btn', function() {
                    const groupId = $(this).data('group-id');
                    const group = self.groups.find(g => g.id === groupId);
                    self.showManageUsersModal(group);
                });
            },

            loadGroups: function() {
                const self = this;
                
                // Use unity service proxy
                unity.notifications.emailGroups.emailGroups.getList().then(function(result) {
                    console.log('Email groups loaded:', result);
                    self.groups = result;
                    self.renderGroups();
                }).catch(function(error) {
                    console.error('Failed to load email groups:', error);
                    abp.notify.error('Failed to load email groups');
                });
            },

            renderGroups: function() {
                const container = $('#emailGroupsContainer');
                container.empty();

                if (this.groups.length === 0) {
                    container.html('<p class="text-muted">No email groups found. Click "Create New Group" to add one.</p>');
                    return;
                }

                const groupsHtml = this.groups.map(group => this.createGroupCard(group)).join('');
                container.html(groupsHtml);
            },

            createGroupCard: function(group) {
                const isStatic = group.type === 'Static';
                const editDisabled = isStatic ? 'disabled' : '';
                const deleteButton = !isStatic ? 
                    `<button class="btn btn-sm btn-outline-danger delete-group-btn" data-group-id="${group.id}">
                        <i class="fa fa-trash"></i> Delete
                    </button>` : '';

                return `
                    <div class="card mb-3">
                        <div class="card-body">
                            <div class="d-flex justify-content-between align-items-start">
                                <div>
                                    <h5 class="card-title">
                                        ${group.name}
                                        ${isStatic ? '<span class="badge bg-secondary ms-2">Static</span>' : '<span class="badge bg-primary ms-2">Dynamic</span>'}
                                    </h5>
                                    <p class="card-text text-muted">${group.description || 'No description'}</p>
                                </div>
                                <div class="btn-group" role="group">
                                    <button class="btn btn-sm btn-outline-primary manage-users-btn" data-group-id="${group.id}">
                                        <i class="fa fa-users"></i> Manage Users
                                    </button>
                                    <button class="btn btn-sm btn-outline-secondary edit-group-btn" data-group-id="${group.id}" ${editDisabled}>
                                        <i class="fa fa-edit"></i> Edit
                                    </button>
                                    ${deleteButton}
                                </div>
                            </div>
                        </div>
                    </div>
                `;
            },

            showCreateGroupModal: function() {
                const self = this;
                const modalHtml = `
                    <div class="modal fade" id="createGroupModal" tabindex="-1">
                        <div class="modal-dialog">
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
                modal.show();

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
                        abp.notify.success('Email group created successfully');
                        modal.hide();
                        self.loadGroups();
                    }).catch(function(error) {
                        console.error('Failed to create email group:', error);
                        abp.notify.error('Failed to create email group');
                    });
                });

                $('#createGroupModal').on('hidden.bs.modal', function() {
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
                        self.loadGroups();
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
                                self.loadGroups();
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
                        <div class="modal-dialog modal-lg">
                            <div class="modal-content">
                                <div class="modal-header">
                                    <h5 class="modal-title">Manage Users - ${group.name}</h5>
                                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                                </div>
                                <div class="modal-body">
                                    <div class="mb-3">
                                        <label class="form-label">Add Users</label>
                                        <div class="input-group">
                                            <input type="text" class="form-control" id="userSearchInput" placeholder="Search users by name or email...">
                                            <button class="btn btn-outline-secondary" type="button" id="searchUsersBtn">
                                                <i class="fa fa-search"></i> Search
                                            </button>
                                        </div>
                                        <div id="userSearchResults" class="mt-2"></div>
                                    </div>
                                    <hr>
                                    <div>
                                        <label class="form-label">Current Group Members</label>
                                        <div id="currentGroupUsers" class="mt-2">
                                            <p class="text-muted">Loading users...</p>
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

                this.loadGroupUsers(group.id);

                $('#searchUsersBtn').on('click', function() {
                    const searchTerm = $('#userSearchInput').val();
                    if (searchTerm) {
                        self.searchUsers(searchTerm, group.id);
                    }
                });

                $('#userSearchInput').on('keypress', function(e) {
                    if (e.which === 13) {
                        $('#searchUsersBtn').click();
                    }
                });

                // Use modal-specific event handlers instead of document-wide
                $('#manageUsersModal').on('click', '.add-user-btn', function() {
                    const userId = $(this).data('user-id');
                    self.addUserToGroup(userId, group.id);
                });

                $('#manageUsersModal').on('click', '.remove-user-btn', function() {
                    const groupUserId = $(this).data('group-user-id');
                    self.removeUserFromGroup(groupUserId, group.id);
                });

                $('#manageUsersModal').on('hidden.bs.modal', function() {
                    $(this).remove();
                });
            },

            loadGroupUsers: function(groupId) {
                const self = this;
                
                console.log('Loading users for group:', groupId);
                
                // Clear the container first to avoid duplicates
                $('#currentGroupUsers').html('<p class="text-muted">Loading users...</p>');
                
                // Always fetch fresh data from backend - no caching
                unity.notifications.emailGroups.emailGroupUsers.getEmailGroupUsersByGroupId(groupId).then(function(groupUsers) {
                    console.log('Fresh group users loaded from backend:', groupUsers);
                    
                    if (!groupUsers || groupUsers.length === 0) {
                        $('#currentGroupUsers').html('<p class="text-muted">No users in this group</p>');
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
                        self.renderGroupUsers(usersWithDetails, groupId);
                    });
                }).catch(function(error) {
                    console.error('Failed to load group users:', error);
                    $('#currentGroupUsers').html('<p class="text-danger">Failed to load group users.</p>');
                });
            },

            renderGroupUsers: function(users, groupId) {
                const container = $('#currentGroupUsers');
                
                // Always clear the container first
                container.empty();
                
                if (!users || users.length === 0) {
                    container.html('<p class="text-muted">No users in this group</p>');
                    return;
                }

                console.log('Rendering group users:', users);
                
                // Create unique list based on userId to prevent duplicates
                const uniqueUsers = [];
                const seenUserIds = new Set();
                
                users.forEach(user => {
                    if (!seenUserIds.has(user.userId)) {
                        seenUserIds.add(user.userId);
                        uniqueUsers.push(user);
                    }
                });

                const usersHtml = uniqueUsers.map(user => `
                    <div class="d-flex justify-content-between align-items-center p-2 border rounded mb-2" data-user-id="${user.userId}">
                        <div>
                            <strong>${user.userName || 'Unknown User'}</strong>
                            <span class="text-muted ms-2">${user.email || ''}</span>
                        </div>
                        <button class="btn btn-sm btn-outline-danger remove-user-btn" data-user-id="${user.userId}" data-group-user-id="${user.id}">
                            <i class="fa fa-times"></i> Remove
                        </button>
                    </div>
                `).join('');

                container.html(usersHtml);
            },

            renderSearchResults: function(users, groupId, resultsContainer) {
                const self = this;
                
                if (!Array.isArray(users)) {
                    users = [];
                }
                
                console.log('Rendering search results for users:', users);
                
                // Get current group users to filter them out
                unity.notifications.emailGroups.emailGroupUsers.getEmailGroupUsersByGroupId(groupId).then(function(groupUsers) {
                    const groupUserIds = (groupUsers || []).map(gu => gu.userId);
                        const availableUsers = users.filter(user => !groupUserIds.includes(user.id));
                        
                        if (availableUsers.length === 0) {
                            resultsContainer.html('<p class="text-muted">No users found or all matching users are already in the group</p>');
                            return;
                        }
                        
                        const usersHtml = availableUsers.map(user => `
                            <div class="d-flex justify-content-between align-items-center p-2 border rounded mb-2">
                                <div>
                                    <strong>${user.userName || user.name || user.displayName || 'Unknown'}</strong>
                                    <span class="text-muted ms-2">${user.email || user.emailAddress || ''}</span>
                                </div>
                                <button class="btn btn-sm btn-outline-primary add-user-btn" data-user-id="${user.id}">
                                    <i class="fa fa-plus"></i> Add
                                </button>
                            </div>
                        `).join('');
                        
                        resultsContainer.html(usersHtml);
                }).catch(function() {
                    // If we can't get group users, show all search results
                    if (users.length === 0) {
                        resultsContainer.html('<p class="text-muted">No users found</p>');
                        return;
                    }
                    
                    const usersHtml = users.map(user => `
                        <div class="d-flex justify-content-between align-items-center p-2 border rounded mb-2">
                            <div>
                                <strong>${user.userName || user.name || user.displayName || 'Unknown'}</strong>
                                <span class="text-muted ms-2">${user.email || user.emailAddress || ''}</span>
                            </div>
                            <button class="btn btn-sm btn-outline-primary add-user-btn" data-user-id="${user.id}">
                                <i class="fa fa-plus"></i> Add
                            </button>
                        </div>
                    `).join('');
                    
                    resultsContainer.html(usersHtml);
                });
            },
            
            searchUsers: function(searchTerm, groupId) {
                const self = this;
                const resultsContainer = $('#userSearchResults');
                
                console.log('SearchUsers called with:', { searchTerm, groupId });
                resultsContainer.html('<p class="text-muted">Searching...</p>');

                // Try direct AJAX call with debugging
                const requestData = {
                    filter: searchTerm,
                    maxResultCount: 10,
                    skipCount: 0
                };
                
                console.log('Making request to /api/identity/users with data:', requestData);
                
                $.ajax({
                    url: '/api/identity/users',
                    method: 'GET',
                    data: requestData,
                    headers: {
                        'RequestVerificationToken': abp.security.antiForgery.getToken()
                    },
                    success: function(response) {
                        console.log('User search response:', response);
                        const users = response.items || response || [];
                        self.renderSearchResults(users, groupId, resultsContainer);
                    },
                    error: function(xhr, status, error) {
                        console.error('User search failed:', {
                            status: xhr.status,
                            statusText: xhr.statusText,
                            responseText: xhr.responseText,
                            error: error
                        });
                    }
                });
            },

            addUserToGroup: function(userId, groupId) {
                const self = this;
                const dto = {
                    userId: userId,
                    groupId: groupId
                };

                unity.notifications.emailGroups.emailGroupUsers.insert(dto).then(function() {
                    abp.notify.success('User added to group');
                    self.loadGroupUsers(groupId);
                    $('#userSearchResults').empty();
                    $('#userSearchInput').val('');
                }).catch(function(error) {
                    console.error('Failed to add user to group:', error);
                    abp.notify.error('Failed to add user to group');
                });
            },

            removeUserFromGroup: function(groupUserId, groupId) {
                const self = this;
                
                unity.notifications.emailGroups.emailGroupUsers.deleteUser(groupUserId).then(function() {
                    abp.notify.success('User removed from group');
                    self.loadGroupUsers(groupId);
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