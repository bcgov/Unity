using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Reporting.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.Reporting
{
    public class FormMetadataServiceTests : GrantManagerApplicationTestBase
    {
        private readonly IFormMetadataService _formMetadataService;
        private readonly IApplicationFormVersionRepository _formVersionRepository;

        public FormMetadataServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _formMetadataService = GetRequiredService<IFormMetadataService>();
            _formVersionRepository = GetRequiredService<IApplicationFormVersionRepository>();
        }     

        #region GetFormComponentMetaDataAsync Tests

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Return_Empty_Components_For_Form_With_No_Fields()
        {
            // Arrange
            var formSchema = @"{
                ""components"": []
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            result.Components.Count.ShouldBe(0);
            result.HasDuplicates.ShouldBeFalse();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Return_Metadata_For_Simple_Field()
        {
            // Arrange
            var formSchema = @"{
                ""components"": [
                    {
                        ""type"": ""textfield"",
                        ""key"": ""firstName"",
                        ""label"": ""First Name"",
                        ""input"": true
                    }
                ]
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            result.Components.Count.ShouldBe(1);
            result.HasDuplicates.ShouldBeFalse();
            
            var firstNameField = result.Components.FirstOrDefault(x => x.Key == "firstName");
            firstNameField.ShouldNotBeNull();
            firstNameField.Type.ShouldBe("textfield");
            firstNameField.Label.ShouldBe("First Name");
            firstNameField.Path.ShouldBe("firstName");
            firstNameField.TypePath.ShouldBe("textfield");
            firstNameField.DataPath.ShouldBe("firstName");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Return_Metadata_For_All_Simple_Fields()
        {
            // Arrange
            var formSchema = @"{
                ""components"": [
                    {
                        ""type"": ""textfield"",
                        ""key"": ""firstName"",
                        ""label"": ""First Name"",
                        ""input"": true
                    },
                    {
                        ""type"": ""email"",
                        ""key"": ""emailAddress"",
                        ""label"": ""Email Address"",
                        ""input"": true
                    },
                    {
                        ""type"": ""number"",
                        ""key"": ""age"",
                        ""label"": ""Age"",
                        ""input"": true
                    }
                ]
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            result.Components.Count.ShouldBe(3);
            result.HasDuplicates.ShouldBeFalse();
            
            var firstNameField = result.Components.FirstOrDefault(x => x.Key == "firstName");
            firstNameField.ShouldNotBeNull();
            firstNameField.Type.ShouldBe("textfield");
            firstNameField.Label.ShouldBe("First Name");
            
            var emailField = result.Components.FirstOrDefault(x => x.Key == "emailAddress");
            emailField.ShouldNotBeNull();
            emailField.Type.ShouldBe("email");
            
            var ageField = result.Components.FirstOrDefault(x => x.Key == "age");
            ageField.ShouldNotBeNull();
            ageField.Type.ShouldBe("number");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Return_Metadata_For_Radio_Option()
        {
            // Arrange
            var formSchema = @"{
                ""components"": [
                    {
                        ""type"": ""radio"",
                        ""key"": ""s01_RadioGroupComponent"",
                        ""label"": ""Select One"",
                        ""input"": true,
                        ""values"": [
                            {
                                ""label"": ""Option 1"",
                                ""value"": ""option1""
                            },
                            {
                                ""label"": ""Option 2"",
                                ""value"": ""option2""
                            }
                        ]
                    }
                ]
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            
            // Radio component itself is filtered out, but options should be present
            var radioOption = result.Components.FirstOrDefault(x => x.Key == "s01_RadioGroupComponent-option1");
            radioOption.ShouldNotBeNull();
            radioOption.Type.ShouldBe("option");
            radioOption.Label.ShouldBe("Option 1");
            radioOption.Path.ShouldBe("s01_RadioGroupComponent->option1");
            radioOption.TypePath.ShouldBe("radio->option");
            radioOption.DataPath.ShouldBe("s01_RadioGroupComponent->option1"); // "s01_RadioGroupComponent" is filtered out because it contains "Group"
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Return_Metadata_For_All_Radio_Options()
        {
            // Arrange
            var formSchema = @"{
                ""components"": [
                    {
                        ""type"": ""radio"",
                        ""key"": ""preferredContact"",
                        ""label"": ""Preferred Contact Method"",
                        ""input"": true,
                        ""values"": [
                            {
                                ""label"": ""Email"",
                                ""value"": ""email""
                            },
                            {
                                ""label"": ""Phone"",
                                ""value"": ""phone""
                            },
                            {
                                ""label"": ""Mail"",
                                ""value"": ""mail""
                            }
                        ]
                    }
                ]
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            result.Components.Count.ShouldBe(3); // Only 3 options (radio group is filtered out)
            
            // Check that we have the expanded options
            result.Components.ShouldContain(x => x.Key == "preferredContact-email" && x.Type == "option");
            result.Components.ShouldContain(x => x.Key == "preferredContact-phone" && x.Type == "option");
            result.Components.ShouldContain(x => x.Key == "preferredContact-mail" && x.Type == "option");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Return_Metadata_For_DataGrid_Field()
        {
            // Arrange
            var formSchema = @"{
                ""components"": [
                    {
                        ""type"": ""datagrid"",
                        ""key"": ""myDataGrid"",
                        ""label"": ""Contact List"",
                        ""input"": true,
                        ""components"": [
                            {
                                ""type"": ""textfield"",
                                ""key"": ""name"",
                                ""label"": ""Name"",
                                ""input"": true
                            },
                            {
                                ""type"": ""email"",
                                ""key"": ""email"",
                                ""label"": ""Email"",
                                ""input"": true
                            }
                        ]
                    }
                ]
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            
            // DataGrid itself is filtered out, but nested fields should be present with their original keys
            var dataGridField = result.Components.FirstOrDefault(x => x.Key == "name");
            dataGridField.ShouldNotBeNull();
            dataGridField.Type.ShouldBe("textfield");
            dataGridField.Label.ShouldBe("Name");
            dataGridField.Path.ShouldBe("myDataGrid->name");
            dataGridField.TypePath.ShouldBe("datagrid->textfield");
            dataGridField.DataPath.ShouldBe("myDataGrid->name");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Return_Metadata_For_DataGrid_Fields()
        {
            // Arrange
            var formSchema = @"{
                ""components"": [
                    {
                        ""type"": ""datagrid"",
                        ""key"": ""contacts"",
                        ""label"": ""Contact List"",
                        ""input"": true,
                        ""components"": [
                            {
                                ""type"": ""textfield"",
                                ""key"": ""name"",
                                ""label"": ""Name"",
                                ""input"": true
                            },
                            {
                                ""type"": ""email"",
                                ""key"": ""email"",
                                ""label"": ""Email"",
                                ""input"": true
                            },
                            {
                                ""type"": ""phoneNumber"",
                                ""key"": ""phone"",
                                ""label"": ""Phone"",
                                ""input"": true
                            }
                        ]
                    }
                ]
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            result.Components.Count.ShouldBe(3); // Only 3 fields (datagrid is filtered out)
            
            // Check datagrid fields (datagrid itself is filtered out, keys are original not compound)
            var nameField = result.Components.FirstOrDefault(x => x.Key == "name");
            nameField.ShouldNotBeNull();
            nameField.Type.ShouldBe("textfield");
            nameField.Path.ShouldBe("contacts->name");
            nameField.TypePath.ShouldBe("datagrid->textfield");
            
            result.Components.ShouldContain(x => x.Key == "email");
            result.Components.ShouldContain(x => x.Key == "phone");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Return_Metadata_For_Nested_Panel_Field()
        {
            // Arrange
            var formSchema = @"{
                ""components"": [
                    {
                        ""type"": ""simplepanel"",
                        ""key"": ""s01_Panel"",
                        ""label"": ""Personal Information"",
                        ""input"": false,
                        ""components"": [
                            {
                                ""type"": ""textfield"",
                                ""key"": ""firstName"",
                                ""label"": ""First Name"",
                                ""input"": true
                            }
                        ]
                    }
                ]
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            
            // Panel itself should NOT be filtered out - "simplepanel" is not in the skipTypes list, only "panel" is
            var nestedField = result.Components.FirstOrDefault(x => x.Key == "firstName");
            nestedField.ShouldNotBeNull();
            nestedField.Type.ShouldBe("textfield");
            nestedField.Label.ShouldBe("First Name");
            nestedField.Path.ShouldBe("s01_Panel->firstName");
            nestedField.TypePath.ShouldBe("simplepanel->textfield");
            nestedField.DataPath.ShouldBe("s01_Panel->firstName"); // Panel should be filtered out from DataPath because "s01_Panel" contains "panel"
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Return_Null_For_NonExistent_Field()
        {
            // Arrange
            var formSchema = @"{
                ""components"": [
                    {
                        ""type"": ""textfield"",
                        ""key"": ""firstName"",
                        ""label"": ""First Name"",
                        ""input"": true
                    }
                ]
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            
            var nonExistentField = result.Components.FirstOrDefault(x => x.Key == "nonExistentField");
            nonExistentField.ShouldBeNull();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Handle_Checkbox_Group_Options()
        {
            // Arrange
            var formSchema = @"{
                ""components"": [
                    {
                        ""type"": ""simplecheckboxes"",
                        ""key"": ""interests"",
                        ""label"": ""Your Interests"",
                        ""input"": true,
                        ""values"": [
                            {
                                ""label"": ""Sports"",
                                ""value"": ""sports""
                            }
                        ]
                    }
                ]
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            result.Components.Count.ShouldBe(2); // checkbox group + 1 option
            
            // Check that we have the checkbox group component (not filtered out)
            var checkboxGroup = result.Components.FirstOrDefault(x => x.Key == "interests");
            checkboxGroup.ShouldNotBeNull();
            checkboxGroup.Type.ShouldBe("simplecheckboxes");
            
            var checkboxOption = result.Components.FirstOrDefault(x => x.Key == "interests-sports");
            checkboxOption.ShouldNotBeNull();
            checkboxOption.Type.ShouldBe("option");
            checkboxOption.Label.ShouldBe("Sports");
            checkboxOption.Path.ShouldBe("interests->sports");
            checkboxOption.TypePath.ShouldBe("simplecheckboxes->option");
            checkboxOption.DataPath.ShouldBe("interests->sports");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Handle_Tabs_Container()
        {
            // Arrange
            var formSchema = @"{
                ""components"": [
                    {
                        ""type"": ""tabs"",
                        ""key"": ""mainTabs"",
                        ""label"": ""Main Tabs"",
                        ""input"": false,
                        ""components"": [
                            {
                                ""label"": ""Tab 1"",
                                ""components"": [
                                    {
                                        ""type"": ""textfield"",
                                        ""key"": ""fieldInTab"",
                                        ""label"": ""Field in Tab"",
                                        ""input"": true
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            result.Components.Count.ShouldBe(1); // Only the field (tabs is filtered out)
            
            var fieldInTab = result.Components.FirstOrDefault(x => x.Key == "fieldInTab");
            fieldInTab.ShouldNotBeNull();
            fieldInTab.Type.ShouldBe("textfield");
            fieldInTab.Label.ShouldBe("Field in Tab");
            fieldInTab.Path.ShouldBe("mainTabs->fieldInTab");
            fieldInTab.TypePath.ShouldBe("tabs->textfield");
            fieldInTab.DataPath.ShouldBe("fieldInTab"); // Tabs should be filtered out from DataPath
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Handle_Mixed_Component_Types()
        {
            // Arrange
            var formSchema = @"{
                ""components"": [
                    {
                        ""type"": ""textfield"",
                        ""key"": ""firstName"",
                        ""label"": ""First Name"",
                        ""input"": true
                    },
                    {
                        ""type"": ""simplecheckboxes"",
                        ""key"": ""interests"",
                        ""label"": ""Interests"",
                        ""input"": true,
                        ""values"": [
                            {
                                ""label"": ""Sports"",
                                ""value"": ""sports""
                            },
                            {
                                ""label"": ""Music"",
                                ""value"": ""music""
                            }
                        ]
                    },
                    {
                        ""type"": ""datagrid"",
                        ""key"": ""references"",
                        ""label"": ""References"",
                        ""input"": true,
                        ""components"": [
                            {
                                ""type"": ""textfield"",
                                ""key"": ""refName"",
                                ""label"": ""Reference Name"",
                                ""input"": true
                            }
                        ]
                    }
                ]
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            result.Components.Count.ShouldBe(5); // firstName + interests + 2 checkbox options + 1 datagrid field (datagrid itself is filtered out)
            
            // Simple field
            var firstNameField = result.Components.FirstOrDefault(x => x.Key == "firstName");
            firstNameField.ShouldNotBeNull();
            firstNameField.Type.ShouldBe("textfield");
            
            // Checkbox group and options
            var interestsField = result.Components.FirstOrDefault(x => x.Key == "interests");
            interestsField.ShouldNotBeNull();
            interestsField.Type.ShouldBe("simplecheckboxes");
            
            var sportsField = result.Components.FirstOrDefault(x => x.Key == "interests-sports");
            sportsField.ShouldNotBeNull();
            sportsField.Type.ShouldBe("option");
            
            var musicField = result.Components.FirstOrDefault(x => x.Key == "interests-music");
            musicField.ShouldNotBeNull();
            musicField.Type.ShouldBe("option");
            
            // DataGrid field (datagrid itself is filtered out)
            var refField = result.Components.FirstOrDefault(x => x.Key == "refName");
            refField.ShouldNotBeNull();
            refField.Type.ShouldBe("textfield");
            refField.Path.ShouldBe("references->refName");
            refField.TypePath.ShouldBe("datagrid->textfield");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Handle_Nested_Containers()
        {
            // Arrange
            var formSchema = @"{
                ""components"": [
                    {
                        ""type"": ""simplepanel"",
                        ""key"": ""personalInfo"",
                        ""label"": ""Personal Information"",
                        ""input"": false,
                        ""components"": [
                            {
                                ""type"": ""textfield"",
                                ""key"": ""firstName"",
                                ""label"": ""First Name"",
                                ""input"": true
                            },
                            {
                                ""type"": ""textfield"",
                                ""key"": ""lastName"",
                                ""label"": ""Last Name"",
                                ""input"": true
                            }
                        ]
                    }
                ]
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            result.Components.Count.ShouldBe(3); // simplepanel + 2 fields (simplepanel is NOT filtered out, only "panel" is)
            
            var panel = result.Components.FirstOrDefault(x => x.Key == "personalInfo");
            panel.ShouldNotBeNull();
            panel.Type.ShouldBe("simplepanel");
            
            var firstNameField = result.Components.FirstOrDefault(x => x.Key == "firstName");
            firstNameField.ShouldNotBeNull();
            firstNameField.Path.ShouldBe("personalInfo->firstName");
            firstNameField.TypePath.ShouldBe("simplepanel->textfield");
            // personalInfo doesn't match container patterns exactly, so it's preserved in DataPath
            firstNameField.DataPath.ShouldBe("personalInfo->firstName"); 
            
            result.Components.ShouldContain(x => x.Key == "lastName");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Handle_Duplicate_Paths()
        {
            // Arrange - Create a form with actual duplicate paths (same key at root level)
            var formSchema = @"{
                ""components"": [
                    {
                        ""type"": ""textfield"",
                        ""key"": ""duplicateField"",
                        ""label"": ""Duplicate Field 1"",
                        ""input"": true
                    },
                    {
                        ""type"": ""textfield"",
                        ""key"": ""duplicateField"",
                        ""label"": ""Duplicate Field 2"",
                        ""input"": true
                    }
                ]
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            result.HasDuplicates.ShouldBeTrue();
            
            // Check that duplicate fields have been prefixed
            var duplicateFields = result.Components.Where(x => x.Key == "duplicateField").ToList();
            duplicateFields.Count.ShouldBe(2);
            
            // Both duplicate paths should have (DKx) prefixes - the algorithm prefixes all duplicates
            var pathsWithDuplicates = result.Components.Where(x => x.Key == "duplicateField" && x.Path.Contains("(DK")).ToList();
            pathsWithDuplicates.Count.ShouldBe(2);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Skip_Non_Input_Components()
        {
            // Arrange
            var formSchema = @"{
                ""components"": [
                    {
                        ""type"": ""textfield"",
                        ""key"": ""validField"",
                        ""label"": ""Valid Field"",
                        ""input"": true
                    },
                    {
                        ""type"": ""button"",
                        ""key"": ""submitButton"",
                        ""label"": ""Submit"",
                        ""input"": false
                    },
                    {
                        ""type"": ""html"",
                        ""key"": ""htmlContent"",
                        ""content"": ""<p>Some content</p>"",
                        ""input"": false
                    },
                    {
                        ""type"": ""textfield"",
                        ""key"": ""anotherValidField"",
                        ""label"": ""Another Valid Field"",
                        ""input"": true
                    }
                ]
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            result.Components.Count.ShouldBe(2);
            result.Components.ShouldContain(x => x.Key == "validField");
            result.Components.ShouldContain(x => x.Key == "anotherValidField");
            result.Components.ShouldNotContain(x => x.Key == "submitButton");
            result.Components.ShouldNotContain(x => x.Key == "htmlContent");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFormComponentMetaDataAsync_Should_Handle_Tabs_And_Filter_DataPath()
        {
            // Arrange
            var formSchema = @"{
                ""components"": [
                    {
                        ""type"": ""tabs"",
                        ""key"": ""mainTabs"",
                        ""label"": ""Main Tabs"",
                        ""input"": false,
                        ""components"": [
                            {
                                ""label"": ""Tab 1"",
                                ""components"": [
                                    {
                                        ""type"": ""textfield"",
                                        ""key"": ""fieldInTab"",
                                        ""label"": ""Field in Tab"",
                                        ""input"": true
                                    },
                                    {
                                        ""type"": ""simplecheckboxes"",
                                        ""key"": ""optionsInTab"",
                                        ""label"": ""Options in Tab"",
                                        ""input"": true,
                                        ""values"": [
                                            {
                                                ""label"": ""Option A"",
                                                ""value"": ""optionA""
                                            }
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }";

            var formVersionId = await CreateFormVersion(formSchema);

            // Act
            var result = await _formMetadataService.GetFormComponentMetaDataAsync(formVersionId);

            // Assert
            result.ShouldNotBeNull();
            result.Components.Count.ShouldBe(3); // fieldInTab + optionsInTab + 1 option (tabs is filtered out)
            
            var fieldInTab = result.Components.FirstOrDefault(x => x.Key == "fieldInTab");
            fieldInTab.ShouldNotBeNull();
            fieldInTab.Path.ShouldBe("mainTabs->fieldInTab");
            fieldInTab.DataPath.ShouldBe("fieldInTab"); // tabs should be filtered out
            
            var optionInTab = result.Components.FirstOrDefault(x => x.Key == "optionsInTab-optionA");
            optionInTab.ShouldNotBeNull();
            optionInTab.Path.ShouldBe("mainTabs->optionsInTab->optionA");
            optionInTab.DataPath.ShouldBe("optionsInTab->optionA"); // both "mainTabs" and "optionsInTab" are filtered out because they contain "tabs" and "tab"
        }

        #endregion

        #region Helper Methods

        private async Task<Guid> CreateFormVersion(string formSchema)
        {
            var formVersion = new ApplicationFormVersion
            {
                ApplicationFormId = GrantManagerTestData.ApplicationForm1_Id,
                FormSchema = formSchema,
                ChefsApplicationFormGuid = Guid.NewGuid().ToString(),
                ChefsFormVersionGuid = Guid.NewGuid().ToString(),
                Version = 1,
                Published = true
            };

            var inserted = await _formVersionRepository.InsertAsync(formVersion, true);
            return inserted.Id;
        }

        #endregion
    }
}