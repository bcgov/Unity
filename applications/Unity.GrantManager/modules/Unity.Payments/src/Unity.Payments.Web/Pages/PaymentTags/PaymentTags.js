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
        init(this);
        initEvents(this);

        this.initialized = true;
        return this;
    }


    
    TagsInput.prototype.addTag = function (tagData) {
        let defaultClass = 'tags-common';
        let id, tagText, tagClass;

        id = tagData.Id;
        tagText = tagData.Name || '';
        tagClass = tagData.class || defaultClass;



        if (this.anyErrors(tagText))
            return;

        this.arr.push({ Id: id, Name: tagText });

        let tagInput = this;

        let tag = document.createElement('span');
        tag.className = this.options.tagClass + ' ' + tagClass;
        tag.innerText = tagText;

        let closeIcon = document.createElement('a');
        closeIcon.innerHTML = '&times;';

        
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
        updateSelectedTagsInput(this.arr)
        return this;
    }

    
    TagsInput.prototype.deleteTag = function (tag, i) {
        let self = this;

        if (this.arr[i].Name === 'Uncommon Tags') {
            abp.message.confirm('Are you sure you want to delete all the uncommon tags?')
                .then(function (confirmed) {
                    if (confirmed) {
                        tag.remove();
                        self.arr.splice(i, 1);
                        self.orignal_input.value = JSON.stringify(self.arr);
                        return self;
                    }
                });
        } else {
            tag.remove();
            this.arr.splice(i, 1);
            this.orignal_input.value = JSON.stringify(this.arr);
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
            this.arr.some(tag => tag.Name === string)
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
        tags.wrapper.classList.add(tags.options.wrapperClass);
        tags.orignal_input.setAttribute('hidden', 'true');
        tags.orignal_input.parentNode.insertBefore(tags.wrapper, tags.orignal_input);
        tags.input.addEventListener('input', function () {
            const inputValue = tags.input.value.trim().toLowerCase();

           
            if (inputValue.length > 1) {
                const suggestions = suggestionsArray.filter(tag =>
                    (tag.Name.toLowerCase()).includes(inputValue));

                
                if (suggestions.length) {
                    displaySuggestions(tags, suggestions);
                } else {
                    removeSuggestions(tags);
                }

            } else {
               
                removeSuggestions(tags);
            }
        });
    }

   
    function displaySuggestions(tags, suggestions) {
        
        removeSuggestions(tags);

        
        const suggestionContainer = document.createElement('div');
        suggestionContainer.classList.add('tags-suggestion-container');
        const suggestionTitleElement = document.createElement('div');
        suggestionTitleElement.className = 'tags-suggestion-title';
        suggestionTitleElement.innerText = 'ALL TAGS';
        suggestionContainer.appendChild(suggestionTitleElement);

        
        suggestions.forEach(suggestion => {
            const suggestionElement = document.createElement('div');
            suggestionElement.className = 'tags-suggestion-element';
            suggestionElement.innerText = typeof suggestion === 'string' ? suggestion : suggestion.Name;

           
            suggestionElement.addEventListener('click', function () {
                tags.addTag(suggestion);
                removeSuggestions(tags);
                tags.input.value = "";
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

    // initialize the Events
    function initEvents(tags) {
        tags.wrapper.addEventListener('click', function () {
            tags.input.focus();
        });

       
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

            s.Name.toLowerCase() === str.toLowerCase()
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
        return this.arr.slice(); // Return a copy of the array to prevent external modification
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
