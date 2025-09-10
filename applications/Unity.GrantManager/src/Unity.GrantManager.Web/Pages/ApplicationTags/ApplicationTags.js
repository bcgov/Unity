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

        // Create the add tag button
        if (abp.auth.isGranted('Unity.Applications.Tags.Create')) {
            this.addButton = document.createElement('button');
            this.addButton.id = "addTagButton";
            this.addButton.type = "button";
            this.addButton.className = "tag-add-button";
            this.addButton.innerHTML = '<i class="fa fa-plus"></i>';

            // Add event listener for the button
            this.addButton.addEventListener('click', () => {
                this.input.focus();
                showAllSuggestions(this);
            });
        }

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
        updateSelectedTagsInput(this.arr)
        return this;
    }

    TagsInput.prototype.deleteTag = function (tag, i) {
        let self = this;

        if (this.arr[i] && this.arr[i].name === 'Uncommon Tags') {
            abp.message.confirm('Are you sure you want to delete all the uncommon tags?')
                .then(function (confirmed) {
                    if (confirmed) {
                        tag.remove();
                        self.arr.splice(i, 1);
                        self.orignal_input.value = JSON.stringify(self.arr);
                        updateSelectedTagsInput(self.arr);
                        return self;
                    }
                });
        } else {
            tag.remove();
            this.arr.splice(i, 1);
            this.orignal_input.value = JSON.stringify(this.arr);
            updateSelectedTagsInput(this.arr);
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
        tags.wrapper.append(tags.input);

        // Add the button after the input field if it was created
        if (tags.addButton) {
            tags.wrapper.append(tags.addButton);
        }

        tags.wrapper.classList.add(tags.options.wrapperClass);
        tags.orignal_input.setAttribute('hidden', 'true');
        tags.orignal_input.parentNode.insertBefore(tags.wrapper, tags.orignal_input);
        
        // Enhanced input event for more responsive filtering
        tags.input.addEventListener('input', function () {
            const inputValue = tags.input.value.trim().toLowerCase();

            // Show filtered suggestions as soon as user starts typing
            if (inputValue.length > 0) {
                // Find all matching tags that contain the input value
                const suggestions = suggestionsArray.filter(tag =>
                    (tag.name.toLowerCase()).includes(inputValue));

                if (suggestions.length) {
                    displaySuggestions(tags, suggestions);
                } else if (suggestionsArray.length > 0) {
                    // If no matches found but we have suggestions, display "No matches" message
                    displayNoMatchesMessage(tags);
                } else {
                    removeSuggestions(tags);
                }
            } else {
                // When input is empty, close the suggestions
                removeSuggestions(tags);
            }
        });

        // Show all suggestions when input field is focused if empty
        tags.input.addEventListener('focus', function() {
            if (tags.input.value.trim() === '') {
                // When input is focused but empty, show all suggestions for better UX
                showAllSuggestions(tags);
            }
        });
    }

    // Function to display a "no matches" message
    function displayNoMatchesMessage(tags) {
        removeSuggestions(tags);

        const suggestionContainer = document.createElement('div');
        suggestionContainer.classList.add('tags-suggestion-container');

        const noMatchesElement = document.createElement('div');
        noMatchesElement.className = 'tags-suggestion-no-match';
        noMatchesElement.innerText = 'No matching tags found';
        suggestionContainer.appendChild(noMatchesElement);

        // Add "Show All" option
        const separator = document.createElement('hr');
        separator.className = 'tags-suggestion-separator';
        suggestionContainer.appendChild(separator);

        const showAllOption = document.createElement('div');
        showAllOption.className = 'tags-suggestion-title';
        showAllOption.innerText = 'SHOW ALL';
        showAllOption.addEventListener('click', function () {
            displaySuggestions(tags, suggestionsArray);
        });
        suggestionContainer.appendChild(showAllOption);
        
        tags.wrapper.appendChild(suggestionContainer);
    }

    function displaySuggestions(tags, suggestions) {
        removeSuggestions(tags);

        const suggestionContainer = document.createElement('div');
        suggestionContainer.classList.add('tags-suggestion-container');

        // Only show title if there are suggestions to display
        if (suggestions.length > 0) {
            const suggestionTitleElement = document.createElement('div');
            suggestionTitleElement.className = 'tags-suggestion-title';
            suggestionTitleElement.innerText = suggestions.length === suggestionsArray.length ? 'ALL TAGS' : 'MATCHING TAGS';
            suggestionContainer.appendChild(suggestionTitleElement);
            
            suggestions.forEach(suggestion => {
                const suggestionElement = document.createElement('div');
                suggestionElement.className = 'tags-suggestion-element';
                suggestionElement.innerText = typeof suggestion === 'string' ? suggestion : suggestion.name;
                suggestionElement.addEventListener('click', function () {
                    tags.addTag(suggestion);
                    removeSuggestions(tags);
                    tags.input.value = "";
                    // Focus input again to allow for continued tag adding
                    tags.input.focus();
                });

                suggestionContainer.appendChild(suggestionElement);
            });
            
            tags.wrapper.appendChild(suggestionContainer);
        }

        // Add "Show All" option if the suggestions aren't already showing all tags
        if (suggestions.length !== suggestionsArray.length && suggestionsArray.length > 0) {
            // Add separator
            const separator = document.createElement('hr');
            separator.className = 'tags-suggestion-separator';
            suggestionContainer.appendChild(separator);

            const showAllOption = document.createElement('div');
            showAllOption.className = 'tags-suggestion-title';
            showAllOption.innerText = 'SHOW ALL';
            showAllOption.addEventListener('click', function () {
                displaySuggestions(tags, suggestionsArray);
            });
            suggestionContainer.appendChild(showAllOption);
        }
    }

    function removeSuggestions(tags) {
        const suggestionContainer = tags.wrapper.querySelector('.tags-suggestion-container');
        if (suggestionContainer) {
            suggestionContainer.remove();
        }
    }

    function showAllSuggestions(tags) {
        if (suggestionsArray && suggestionsArray.length > 0) {
            displaySuggestions(tags, suggestionsArray);
        }
    }

    function initEvents(tags) {
        tags.wrapper.addEventListener('click', function (e) {
            // Only focus if the click wasn't on a tag element
            if (!e.target.classList.contains(tags.options.tagClass.replace('.', ''))) {
                tags.input.focus();
            }
        });

        // Close suggestions when clicking outside the tag input area
        document.addEventListener('click', function(e) {
            if (!tags.wrapper.contains(e.target) && !e.target.closest('.tags-suggestion-container')) {
                removeSuggestions(tags);
            }
        });

        tags.input.addEventListener('focusout', function (e) {
            // Don't close suggestions if clicking inside the suggestions container
            if (e.relatedTarget?.closest('.tags-suggestion-container')) {
                return;
            }
            
            // Allow time for click events on suggestions to fire before removing them
            setTimeout(() => {
                if (document.activeElement !== tags.input && 
                    !document.activeElement.closest('.tags-suggestion-container')) {
                    removeSuggestions(tags);
                }
            }, 100);

            $('#assignTagsModelSaveBtn').click(function () {
                trimAndAddTag(tags);
            });
        });

        tags.input.addEventListener('keydown', function (e) {
            if (~[9, 13, 188, 32].indexOf(e.keyCode)) {
                e.preventDefault();
                trimAndAddTag(tags);
                removeSuggestions(tags);
            } else if (e.keyCode === 27) { // ESC key
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
