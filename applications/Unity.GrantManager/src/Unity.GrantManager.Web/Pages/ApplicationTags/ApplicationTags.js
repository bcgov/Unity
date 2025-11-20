$(function () {

    let suggestionsArray = [];

    let TagsInput = function (opts) {
        this.options = Object.assign(TagsInput.defaults, opts);
        this.init();
    }

    TagsInput.prototype.init = function (opts) {
        this.options = opts ? Object.assign(this.options, opts) : this.options;

        if (this.initialized)
            this.destroy();
        this.orignal_input = document.getElementById(this.options.selector);

        if (!this.orignal_input) {
            console.error("tags-input couldn't find an element with the specified ID");
            return this;
        }

        this.arr = [];

        this.wrapper = document.createElement('div');
        this.input = document.createElement('input');
        this.input.id = "tags-input-control";
        init(this);
        initEvents(this);

        // Disable the input if user doesn't have create permission
        if (!abp.auth.isGranted('Unity.Applications.Tags.Create')) {
            this.input.disabled = true;
            this.input.type = "hidden";
            this.wrapper.classList.add('tags-input-disabled');
        }

        this.initialized = true;
        return this;
    }

    TagsInput.prototype.addTag = function (tagData) {
        let defaultClass = 'tags-common';
        let id, tagText, tagClass;

        id = tagData.id;
        tagText = tagData.name || '';
        tagClass = tagData.class || defaultClass;

        if (this.anyErrors(tagText))
            return;

        this.arr.push({ Id: id, Name: tagText });

        let tagInput = this;

        let tag = document.createElement('span');
        tag.className = this.options.tagClass + ' ' + tagClass;
        tag.innerText = tagText;

        if (abp.auth.isGranted('Unity.Applications.Tags.Delete')) {
            let closeIcon = document.createElement('a');
            closeIcon.innerHTML = '&times;';

            closeIcon.addEventListener('click', function (e) {
                e.preventDefault();
                let tag = this.parentNode;

                let tagIndex = Array.from(tagInput.wrapper.childNodes).indexOf(tag);
                if (tagIndex !== -1) {
                    tagInput.deleteTag(tag, tagIndex);
                }
            })

            tag.appendChild(closeIcon);
        }

        this.wrapper.insertBefore(tag, this.input);
        this.orignal_input.value = JSON.stringify(this.arr);
        updateSelectedTagsInput(this.arr);

        return this;
    }

    TagsInput.prototype.deleteTag = function (tag, i) {
        let self = this;

        if (this.arr[i] && this.arr[i].Name === 'Uncommon Tags') {
            abp.message.confirm('Are you sure you want to delete all the uncommon tags?')
                .then(function (confirmed) {
                    if (confirmed) {
                        tag.remove();
                        self.arr.splice(i, 1);
                        self.orignal_input.value = JSON.stringify(self.arr);
                        updateSelectedTagsInput(self.arr);
                        
                        // Expand input if no tags remain
                        if (self.arr.length === 0) {
                            self.input.classList.add('expanded');
                        }
                        
                        return self;
                    }
                });
        } else {
            tag.remove();
            this.arr.splice(i, 1);
            this.orignal_input.value = JSON.stringify(this.arr);
            updateSelectedTagsInput(this.arr);
            
            // Expand input if no tags remain
            if (this.arr.length === 0) {
                this.input.classList.add('expanded');
            }
            
            return this;
        }
    }

    TagsInput.prototype.anyErrors = function (string) {
        if (this.options.max != null && this.arr.length >= this.options.max) {
            console.log('max tags limit reached');
            return true;
        }

        if (
            !this.options.duplicate &&
            this.arr.some(tag => tag.name === string)
        ) {
            console.log('duplicate found "' + string + '"');
            return true;
        }

        return false;
    }

    TagsInput.prototype.addData = function (array) {
        let plugin = this;

        array.forEach(function (string) {
            plugin.addTag(string);
        })
        return this;
    }

    TagsInput.prototype.getInputString = function () {
        return this.arr.join(',');
    }

    TagsInput.prototype.setSuggestions = function (sugArray) {
        suggestionsArray = sugArray;
    }

    TagsInput.prototype.destroy = function () {
        this.orignal_input.removeAttribute('hidden');

        delete this.orignal_input;
        let self = this;

        Object.keys(this).forEach(function (key) {
            if (self[key] instanceof HTMLElement)
                self[key].remove();

            if (key != 'options')
                delete self[key];
        });

        this.initialized = false;
    }

    function init(tags) {
        // Create and append the add tag button
        tags.addButton = document.createElement('button');
        tags.addButton.type = 'button';
        tags.addButton.className = 'tags-add-button';
        tags.addButton.innerHTML = '+';
        tags.addButton.title = 'Add a tag';
        
        // Disable button if user doesn't have create permission
        if (!abp.auth.isGranted('Unity.Applications.Tags.Create')) {
            tags.addButton.disabled = true;
            tags.addButton.style.display = 'none';
        }
        
        tags.wrapper.append(tags.input);
        tags.wrapper.append(tags.addButton);
        tags.wrapper.classList.add(tags.options.wrapperClass);
        tags.orignal_input.setAttribute('hidden', 'true');
        tags.orignal_input.parentNode.insertBefore(tags.wrapper, tags.orignal_input);
        tags.input.setAttribute('placeholder', 'Add a tag...');

        tags.input.addEventListener('input', function () {
            const inputValue = tags.input.value.trim().toLowerCase();

            if (inputValue.length === 1) {
                // Filter by first character only
                const suggestions = suggestionsArray.filter(tag =>
                    tag.name.toLowerCase().startsWith(inputValue));

                if (suggestions.length) {
                    displaySuggestions(tags, suggestions, true);
                } else {
                    removeSuggestions(tags);
                }
            } else if (inputValue.length > 1) {
                // Filter by checking if input appears anywhere in tag name
                const suggestions = suggestionsArray.filter(tag =>
                    (tag.name.toLowerCase()).includes(inputValue));

                if (suggestions.length) {
                    displaySuggestions(tags, suggestions, true);
                } else {
                    removeSuggestions(tags);
                }
            } else {
                removeSuggestions(tags);
            }
        });
        
        // Expand input on focus
        tags.input.addEventListener('focus', function () {
            tags.input.classList.add('expanded');
        });
    }

    function displaySuggestions(tags, suggestions, isFiltered) {

        removeSuggestions(tags);

        const suggestionContainer = document.createElement('div');
        suggestionContainer.classList.add('tags-suggestion-container');
        const suggestionTitleElement = document.createElement('div');
        suggestionTitleElement.className = 'tags-suggestion-title';
        suggestionTitleElement.innerText = isFiltered ? 'FILTERED TAGS' : 'ALL TAGS';
        suggestionContainer.appendChild(suggestionTitleElement);
        suggestions.forEach(suggestion => {
            const suggestionElement = document.createElement('div');
            suggestionElement.className = 'tags-suggestion-element';
            suggestionElement.innerText = typeof suggestion === 'string' ? suggestion : suggestion.name;
            suggestionElement.addEventListener('click', function () {
                tags.addTag(suggestion);
                removeSuggestions(tags);
                tags.input.value = "";
                tags.wrapper.focus();
            });

            suggestionContainer.appendChild(suggestionElement);
        });

        tags.wrapper.appendChild(suggestionContainer);
    }

    function removeSuggestions(tags) {
        const suggestionContainer = tags.wrapper.querySelector('.tags-suggestion-container');
        if (suggestionContainer) {
            suggestionContainer.remove();
        }
    }

    function initEvents(tags) {
        // Capture keystrokes anywhere in the wrapper and focus input
        let tagApplicationsModalElem = $("#tagApplicationsModal")[0];
        if (tagApplicationsModalElem) {
            tagApplicationsModalElem.addEventListener('keydown', function (e) {
                // Skip if input is already focused or if it's a special key
                if (document.activeElement === tags.input) {
                    return;
                }

                // Check if it's a printable character
                const isPrintableKey = e.key.length === 1 && !e.ctrlKey && !e.altKey && !e.metaKey;
                if (isPrintableKey && abp.auth.isGranted('Unity.Applications.Tags.Create')) {
                    // Expand and focus the input
                    tags.input.classList.add('expanded');
                    tags.input.focus();
                }
            });
        }


        // Add button click event - show all tags
        if (tags.addButton) {
            tags.addButton.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                
                // Toggle suggestions display
                const existingSuggestions = tags.wrapper.querySelector('.tags-suggestion-container');
                if (existingSuggestions) {
                    removeSuggestions(tags);
                } else {
                    // Expand input and show all suggestions sorted alphabetically
                    tags.input.classList.add('expanded');
                    const sortedSuggestions = [...suggestionsArray].sort((a, b) => 
                        a.name.localeCompare(b.name)
                    );
                    displaySuggestions(tags, sortedSuggestions, false);
                    // Focus the input field so user can type to filter
                    tags.input.focus();
                }
            });
        }

        tags.input.addEventListener('focusout', function () {
            $('#assignTagsModelSaveBtn').click(function () {
                trimAndAddTag(tags);
            })
        });

        tags.input.addEventListener('keydown', function (e) {
            if (~[9, 13, 188, 32].indexOf(e.keyCode)) {
                e.preventDefault();
                trimAndAddTag(tags);
                removeSuggestions(tags);

            }
        });
    }

    function trimAndAddTag(tags) {
        let str = tags.input.value.trim();
        if (!str) {
            tags.input.value = "";
            return;
        }

        const matched = suggestionsArray.find(s =>
            s.name.toLowerCase() === str.toLowerCase()
        );

        if (matched) {
            tags.addTag(typeof matched === 'string' ? { name: matched } : matched);
        } else {
            abp.message.warn('Please select a tag from the suggestions.');
        }

        tags.input.value = "";
    }

    function updateSelectedTagsInput(tagsArray) {
        let jsonValue = JSON.stringify(tagsArray);
        $('#SelectedTagsJson').val(jsonValue);
    }

    TagsInput.prototype.getTags = function () {
        return this.arr.slice();
    }

    TagsInput.defaults = {
        selector: '',
        wrapperClass: 'tags-input-wrapper',
        tagClass: 'tag',
        max: null,
        duplicate: false
    }

    window.TagsInput = TagsInput;
});
