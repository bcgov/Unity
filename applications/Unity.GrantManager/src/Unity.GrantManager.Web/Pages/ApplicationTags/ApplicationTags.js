$(function () {

    let suggestionsArray = [];
   

    // Plugin Constructor
    let TagsInput = function (opts) {
        this.options = Object.assign(TagsInput.defaults, opts);
        this.init();
    }
    // Initialize the plugin
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
        init(this);
        initEvents(this);

        this.initialized = true;
        return this;
    }


    // Add Tags
    TagsInput.prototype.addTag = function (tagData) {
        let defaultClass = 'tags-common';
        let tagText, tagClass;

        if (typeof tagData === 'string') {
            tagText = tagData;
            tagClass = defaultClass;
           
        } else {
            tagText = tagData.text || '';
            tagClass = tagData.class || defaultClass;
     
        }

        if (this.anyErrors(tagText))
            return;

        this.arr.push(tagText);

        let tagInput = this;

        let tag = document.createElement('span');
        tag.className = this.options.tagClass + ' ' + tagClass;
        tag.innerText = tagText;

        let closeIcon = document.createElement('a');
        closeIcon.innerHTML = '&times;';

        // delete the tag when icon is clicked
        closeIcon.addEventListener('click', function (e) {
            e.preventDefault();
            let tag = this.parentNode;

            for (let i = 0; i < tagInput.wrapper.childNodes.length; i++) {
                if (tagInput.wrapper.childNodes[i] == tag)
                    tagInput.deleteTag(tag, i);
            }
        })

        tag.appendChild(closeIcon);
        this.wrapper.insertBefore(tag, this.input);
        this.orignal_input.value = JSON.stringify(this.arr);

        return this;
    }

    // Delete Tags
    TagsInput.prototype.deleteTag = function (tag, i) {
        let self = this;
        if (this.arr[i] == 'Uncommon Tags') {
            abp.message.confirm('Are you sure to delete all the uncommon tags?')
                .then(function (confirmed) {
                    if (confirmed) {
                        tag.remove();
                        self.arr.splice(i, 1);
                        self.orignal_input.value = JSON.stringify(self.arr);
                        return self;
                    }

                });
        }
        else {
            tag.remove();
            this.arr.splice(i, 1);
            this.orignal_input.value = JSON.stringify(this.arr);
            return this;
        }

    }

    // Make sure input string have no error with the plugin
    TagsInput.prototype.anyErrors = function (string) {
        if (this.options.max != null && this.arr.length >= this.options.max) {
            console.log('max tags limit reached');
            return true;
        }

        if (!this.options.duplicate && this.arr.indexOf(string) != -1) {
            console.log('duplicate found " ' + string + ' " ')
            return true;
        }

        return false;
    }

    // Add tags programmatically 
    TagsInput.prototype.addData = function (array) {
        let plugin = this;

        array.forEach(function (string) {
            plugin.addTag(string);
        })
        return this;
    }

    // Get the Input String
    TagsInput.prototype.getInputString = function () {
        return this.arr.join(',');
    }
    TagsInput.prototype.setSuggestions = function (sugArray) {
        suggestionsArray = sugArray;
    }


    // destroy the plugin
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

    // Private function to initialize the tag input plugin
    function init(tags) {
        tags.wrapper.append(tags.input);
        tags.wrapper.classList.add(tags.options.wrapperClass);
        tags.orignal_input.setAttribute('hidden', 'true');
        tags.orignal_input.parentNode.insertBefore(tags.wrapper, tags.orignal_input);
        tags.input.addEventListener('input', function () {
            const inputValue = tags.input.value.trim().toLowerCase();

            // Show suggestions only after the first character entry
            if (inputValue.length > 1) {
                const suggestions = suggestionsArray.filter(tag => tag.toLowerCase().includes(inputValue));

                // Display suggestions below the input element
                if (suggestions.length) {
                    displaySuggestions(tags, suggestions);
                } else {
                    removeSuggestions(tags);
                }

            } else {
                // Remove suggestions if input is empty
                removeSuggestions(tags);
            }
        });
    }

    // Function to display auto-completion suggestions
    function displaySuggestions(tags, suggestions) {
        // Remove previous suggestions
        removeSuggestions(tags);

        // Create suggestion container
        const suggestionContainer = document.createElement('div');
        suggestionContainer.classList.add('tags-suggestion-container');
        const suggestionTitleElement = document.createElement('div');
        suggestionTitleElement.className = 'tags-suggestion-title';
        suggestionTitleElement.innerText = 'ALL TAGS';
        suggestionContainer.appendChild(suggestionTitleElement);

        // Add suggestions to the container
        suggestions.forEach(suggestion => {
            const suggestionElement = document.createElement('div');
            suggestionElement.className = 'tags-suggestion-element';
            suggestionElement.innerText = suggestion;

            // Add click event to add suggestion as a new tag
            suggestionElement.addEventListener('click', function () {
                tags.addTag(suggestion);
                removeSuggestions(tags);
                tags.input.value = "";
            });

            suggestionContainer.appendChild(suggestionElement);
        });

        // Append the suggestion container below the input
        tags.wrapper.appendChild(suggestionContainer);
    }

    // Function to remove auto-completion suggestions
    function removeSuggestions(tags) {
        const suggestionContainer = tags.wrapper.querySelector('.tags-suggestion-container');
        if (suggestionContainer) {
            suggestionContainer.remove();
        }
    }

    // initialize the Events
    function initEvents(tags) {
        tags.wrapper.addEventListener('click', function () {
            tags.input.focus();
        });

        // for saving tags that are typed, but not added as a chip/pill
        tags.input.addEventListener('focusout', function () {
            $('#applicationTagsModelSaveBtn').click(function () {
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
        if (str != "") {
            tags.addTag(str);
        }
        tags.input.value = "";
    }

    TagsInput.prototype.getTags = function () {
        return this.arr.slice(); // Return a copy of the array to prevent external modification
    }

    // Set All the Default Values
    TagsInput.defaults = {
        selector: '',
        wrapperClass: 'tags-input-wrapper',
        tagClass: 'tag',
        max: null,
        duplicate: false
    }

    window.TagsInput = TagsInput;


});


