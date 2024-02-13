$(function () {

    let suggestionsArray = [];


    // Plugin Constructor
    let UserTagsInput = function (opts) {
        this.options = Object.assign(UserTagsInput.defaults, opts);
        this.init();
    }
    // Initialize the plugin
    UserTagsInput.prototype.init = function (opts) {
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
        this.input.classList.add('user-tags-input');
        this.input.setAttribute("id", "user-tags-input");
        this.input.setAttribute("placeholder", "Start typing their name");
        this.input.focus();
        init(this);
        

        this.initialized = true;
        return this;
    }


    // Add Tags
    UserTagsInput.prototype.addTag = function (tagData) {
        let defaultClass = 'tags-common';
        let tagText, tagClass, Id, dutyText;


        tagText = tagData.FullName || '';
        tagClass = tagData.class || defaultClass;
        Id = tagData.Id;
        dutyText = tagData.Duty || '';



        if (this.anyErrors(Id))
            return;


        // Push the tag and duty to this.arr
        this.arr.push({ Id: Id, Duty: dutyText });

        let tagInput = this;

        let tag = document.createElement('dev');
        tag.className = this.options.tagClass + ' ' + tagClass;

        let innerDiv = document.createElement('div');

        let label = document.createElement('label');
        label.textContent = tagText;

        let dutyInput = document.createElement('input');
        dutyInput.type = 'text';
        dutyInput.placeholder = 'Add their duties';
        dutyInput.value = dutyText;
        dutyInput.classList.add('user-tags-duty-input');
        dutyInput.addEventListener('blur', function () {
            // Update duty value in this.arr when input field loses focus
            let index = Array.from(tagInput.wrapper.childNodes).indexOf(tag);
            tagInput.arr[index].Duty = dutyInput.value.trim();
            tagInput.orignal_input.value = JSON.stringify(tagInput.arr);
        });
        let lineBreak = document.createElement('br');
        innerDiv.appendChild(label);
        innerDiv.appendChild(lineBreak);
        innerDiv.appendChild(dutyInput);

        let closeDiv = document.createElement('div');
        closeDiv.className = 'tag tags-close-wraper';

        let closeIcon = document.createElement('a');
        closeIcon.innerHTML = '&times;';
        closeIcon.classList.add('user-tags-close');

        // delete the tag when icon is clicked
        closeIcon.addEventListener('click', function (e) {
            e.preventDefault();
            let tag = this.parentNode.parentNode;

            for (let i = 0; i < tagInput.wrapper.childNodes.length; i++) {
                if (tagInput.wrapper.childNodes[i] == tag)
                    tagInput.deleteTag(tag, i);
            }
        })

        closeDiv.appendChild(closeIcon);
        tag.appendChild(innerDiv);
        tag.appendChild(closeDiv);

        this.wrapper.insertBefore(tag, this.input);
        this.orignal_input.value = JSON.stringify(this.arr);
        this.input.focus();
        return this;
    }

    // Delete Tags
    UserTagsInput.prototype.deleteTag = function (tag, i) {
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
    UserTagsInput.prototype.anyErrors = function (string) {
        if (this.options.max != null && this.arr.length >= this.options.max) {
            console.log('max tags limit reached');
            return true;
        }

        if (!this.options.duplicate && this.arr.some(obj => obj.Id === string)) {
            console.log('duplicate found " ' + string + ' " ');
            return true;
        }

        return false;
    }

    // Add tags programmatically 
    UserTagsInput.prototype.addData = function (array) {
        let plugin = this;

        array.forEach(function (string) {
            plugin.addTag(string);
        })
        return this;
    }

    // Get the Input String
    UserTagsInput.prototype.getInputString = function () {
        return this.arr.join(',');
    }
    UserTagsInput.prototype.setSuggestions = function (sugArray) {
        suggestionsArray = sugArray;
    }


    // destroy the plugin
    UserTagsInput.prototype.destroy = function () {
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
                const suggestions = suggestionsArray.filter(tag => tag.FullName.toLowerCase().includes(inputValue));

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
        suggestionTitleElement.innerText = 'ALL USERS';
        suggestionContainer.appendChild(suggestionTitleElement);

        // Add suggestions to the container
        suggestions.forEach(suggestion => {
            const suggestionElement = document.createElement('div');
            suggestionElement.className = 'tags-suggestion-element';
            suggestionElement.innerText = suggestion.FullName;

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

  
    UserTagsInput.prototype.getTags = function () {
        return this.arr.slice(); // Return a copy of the array to prevent external modification
    }

    // Set All the Default Values
    UserTagsInput.defaults = {
        selector: '',
        wrapperClass: 'user-tags-input-wrapper',
        tagClass: 'user-tag',
        max: null,
        duplicate: false
    }

    window.UserTagsInput = UserTagsInput;


});


