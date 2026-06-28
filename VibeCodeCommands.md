
# Vibe-code Commands

Here are some the vibe code commands I used.

# 1. General

- Update the title to show app title and page in the form "App name - Page name"

## 1.1. Helper projects - ZeeFileSystem

- Create a WinUI Class Library project called ZeeFileSystem.
- Rename ZeeFileSystem to CyberFeedForward.Tools.ZeeFileSystem
- Move all API calls that interact with the Windows File system to ZeeFileSystem

# 2. Layout

- Add Home page
- Add a MenuBar at the top of the app
- Add a CommandBar at the top of the app, below the MenuBar
- Move the CommandBar to the left. Add a toggle to the settings to allow the user to move between left and right.

- Add back and forward Navigation buttons to the command bar, to navigate to visited pages
- Remember the last position and size of the app when it closes

# 3. Pages

## 3.1. Settings

- Add a page called Settings. Navigate to this page when a user clicks on Settings button
- Add a toggle to Settings. When on, this will activate dark mode
- Add System Default as an option for the Dark mode setting
- Create a viewmodel for SettingsPage
- Highlight FolderContentsDivider whenever the cursor is over it
- Create a Tab panel in SettingsPage called SettingsGroups. Create 3 panels named General, Archives, Icons.
- Remove the 'Add new tabs' button and 'Close Tab' buttons from SettingsGroups
- Add NamedIconControl to IconsSettingsTab

### 3.2.1. Settings - General - Startup

- Add a toggle to GeneralSettingsControl called SetStartup. If input parameter is true, app will start on system reboot. Set default to true.
- Add a method to FirstRunService to delete all settings.
- When a user tries to close the app, check if SetStartupToggleSwitch is set to true. If true, close window and let app run in the background. The first time, warn user app is running in the background. If false, close app
- Add Open menu item to tray app menu. If app is closed, open it. If minimize, unminimize it
- Remember state of SetStartupToggleSwitch when app closes
- When FirstRunCustomizationDialog closes, add InitialArchivePath as a new Archive path to list of archives
- Add a drive letter dropdown into FirstRunCustomizationDialog. List available drive letters.
- Add image of icon pointed to by InitialIconPathTextBox. Update image when InitialIconPathTextBox changes
- 

### 3.3.2. Settings - Archives (ArchiveListControl)

- Add a list control in SettingsPage called ArchiveListControl. This control will display a list of file paths. This list will be remembered between sessions.
- Create a User control called ArchiveListControl. Include viewmodel. Add it to the settings panel.
- Add a button to ArchiveListControl. This will allow users to navigate the file system and populate the NewArchivePathTextBox box.
- In ArchiveListControl, add a button in each item in ArchivesListView, allowing a user to remove the item.
- Add the Documents folder as a default to ArchiveListControl
- Add a message in StatusBarText when a user adds an existing folder path to ArchiveListControl
- Add Ready message in StatusBarText when a path is successfully added to ArchiveListControl list
- When a person types in a folder path in NewArchivePathTextBox, verify that the folder. If it exists, add and show a message in StatusBarText saying "Folder Added". Show error message if operation fails.
- When the folder selector returns a real file path, add it to the file list in ArchiveListControl
- Add a plus icon to the add button on ArchiveListControl
- Add a folder browser icon to the Browse button on ArchiveListControl
- Show warning message when user deletes a folder path. If operation successful, add message 'Folder Deleted' to StatusBarText
- Sort folder paths when a user adds a new folder path to list in ArchiveListControl
- Disable AddArchiveButton when folder path is empty
- Do not show path in NewArchivePathTextBox when a folder is selected with the folder selector dialog
- Move code for creating a new archive from NewArchiveButton_OnClick to a method in ArchiveListControlViewModel
- For each archive in ArchivesListView, include archive name and drive letter
- Unmap drive when RemoveArchiveItemButton clicked
- Show drive icon for each archive in ArchivesListView
- The Drive icon and name isn't showing in Windows file explorer
- Add an edit button to each row of ArchivesListView. This will allow users to edit the
- I mapped a folder as a drive, but the drive is empty

#### 3.3.2.1. Dialog - NewArchiveDialog

- Add a Button to ArchiveListControl called 'New Archive'. When clicked, will open a dialog called NewArchiveDialog. This dialog will contain a field for a folder path, a dropdown containing a list of unused drive letters, and a save and cancel button.
- Enable NewArchiveButton only when there is at least 1 unused drive letters
- Add a folder selection dialog button to NewArchiveDialog. Fill in FolderPathTextBox with returned path.
- When Save on NewArchiveDialog is clicked, map the returned folder as a drive with the specified drive letter. Add AppIcon.ico as the drive icon
- Show user friendly error when drive mapping fails
- Make sure all required capabilities and declarations are set.

### 3.4.3.1. Page - Settings - Icons (NamedIconControl)

- Create a user control called NamedIconControl. Include a viewmodel
- Add a table to NamedIconControl. This will be populated from a JSON file. Each row will contain an image from an icon file, and a text box. Below the table will be a save button. The file will be saved to ProgramData
- Add row to the top of NamedIconControl. This row will have a text box called CustomIcons and a save button. By default, CustomIcons will contain a folder path to a sub-folder in Documents called CustomIcons. If the folder doesn't exist, create it, then copy Folder.ico there. 
- This is above the table, and separate from the table. It will contain a file path to a sub folder in Documents called CustomIcons. Store this path as a setting within the project. If the folder doesn't exist, create it.
- Add a button to the right of CustomIconsTextBox. This button will return a folder path for CustomIconsTextBox.
- Add save button to CustomIconsPathGrid, to the right of CustomIconsBrowseButton. Enable this button when the entered folder path differs from the saved path.
- Create a sub-folder in ProgramData to store windows icons. Add an icon called Default.ico. 
- Add a default row of data to ItemsTable. For the icon, add a default file path with a default Image.ico file. 
- Add a table to NamedIconControl named IconList. For each file in CustomIcons folder, add the image of the icon, followed by the name.
- Only show *.ico files in NamedIconSettings
- Add a button to NamedIconSettings with a Open File image. When clicked, open a new file explorer window, opened to the CustomIcons folder.
- Refresh NamedIconSettings when contents of CustomIcons changes
- When CustomIconsSaveButton is pressed, rename CustomIcons. Do not just create a new folder. If new folder exist, warn user. Ask them if they want to merge content.

- Create a method in FolderTools in AppTools called LoadDefaultIcons. This will copy all icons in the Icons folder to CustomIconsFolderPath
- Add a button in NamedIconSettings control called LoadDefaultIcons. When clicked, copy call LoadDefaultIcons method
- Update IconList when files CustomIcons folder changes

## 3.3.4.1. Page - Settings - About

- Create a User Control called AboutControl.
- Add a new tab to SettingsStackPanel to house AboutControl.

# On Startup

- Is there a method that is run when the app is first installed?
- Create a FirstRun service that runs on app startup. When running for the first time, open a dialog for customizing the app.
- Add a flag to GeneralSettings. When clicked, FirstRun will be set to true. This will show FirstRunCustomizationDialog next time the app is run.

- When FirstRunCustomizationDialog runs, load default settings
- Add field for selecting initial archive path. Include a folder selector.
- Add field for selecting initial CustomIcons path. Include a folder selector.
- Save all settings when FirstRunCustomizationDialog closes
- Copy default CustomIcons icons when FirstRunCustomizationDialog closes, 

- When FirstRunCustomizationDialog closes, create the CustomIcons folder and copy icons from AppTools.Icons to the folder.
- Replace the Save and Skip buttons in FirstRunCustomizationDialog with OK button

---

## Controls - Breadcrumb, BreadcrumbBar

- Create a user control called Breadcrumb. It will take a folder path and a list of strings. The string will display the folder name and an arrow icon to the right
- Create a user control called BreadcrumbBar. Given a folder path, it will display each folder in the path using the Breadcrumb control for each folder. For the list of strings, include the sub folders in the folder
- Add the BreadcrumbBar to the top of HomePage.
- Cause a property change event to fire when FolderPath in HomePageViewModel gets updated
- FolderPath in BreadcrumbBar control isn't responding to a file path change in HomePage
- When a person clicks on a menu item in the BreadcrumbBar, update the file path
- Only show the folder dropdown list when a user clicks on BreadcrumbArrowIcon.
- When use clicks on BreadcrumbText, navigate to the specified folder. The exception is the last breadcrumb, since the path is the same.

## Controls - FolderContentsControl

- Create a user control called FolderContentsControl. It will display a list of files and folders, given a folder path place it in the appropriate location
- Update BreadcrumbBar when clicks on a file in FolderContentsControl
- Update FolderContentsControl to only show folders
- Show message '< Empty>' when folder list in FolderContentsControl is empty
- Color the folder icons in FolderContentsControl folder yellow
- Fill in the folder icons in FolderContentsControl folder yellow. Make icon border slightly darker

## Controls - FolderTreeViewControl (Not used yet)

- Create a user control called FolderTreeViewControl. Given a folder path, it will display a tree view of child files and folders

## Controls - Status Bar

- Add a status bar at the bottom of the app

## Toolbars

- Rename TopCommandBar to MainCommandBar
- Add a new toolbar called File. Make it dockable
- Add a Settings button to the right of the menu bar. Use the icon only. Right justify it
- Do not add a page to the navigation stack if the current page is the last page pushed onto the stack

## Library - FolderTools

- Add a method in AppTools.FolderTools.cs called MapDrive. Given a folder path, a drive letter, and a nama, it will create a mapped drive. Return error code if operation fails.
- Add a method in AppTools.FolderTools.cs called UnmapDrive. Given a drive letter, un map the drive. Return a status flag.
- Add a method in AppTools.FolderTools.cs called UpdateFolderIcon. It will take a path to an Icon file and a folder path. When called, update the folder icon with the supplied icon

## Library - FileTools

- Add a method in AppTools.FileTools.cs called SaveIcon. Given an Icon, and a file path, save the icon to file
- Add a method to AppTools.FileTools.cs called IsIdentical. Given 2 file paths, check if the files contents are the same. Optimize method for speed.

## Library - EncryptionTools

- Add a method to AppTools.EncryptionTools.cs called EncryptFile. It will encrypt a file specified by an input file path and save it to a specified location
- Add a method to AppTools.EncryptionTools.cs called DecryptFile. It will decrypt a file specified by an input file path and save it to a specified location

## Library - ImageTools

- Add a method in Tools.ImageTools.ca called ToIcon. Given the path to an image, create a windows icon. The method will return this icon

## Page - Home Page

- Place a divider between FolderContentsPanel and HomeStackPanel. make it movable. 
- Show a left-right cursor icon when the cursor is over FolderContentsDivider
- There are *.ico files in AppTools.Icons folder. However, no icons are being copied when LoadDefaultIcons is called
- Make IconListRowName editable.
- Add a save button to each row in IconList with a check mark. When clicked rename file with original name to new name in the CustomIcons folder. Show error in a popup if operation fails.
- Add revert button to each row in IconList with an X mark. When clicked, will revert text in IconListRowName.
- Enable IconListRowSaveButton and IconListRowRevertButton when text in IconListRowName changes
- Add content of SettingsPage into a scroll panel

## Page - About

- Add an about page to the app. Navigate to it when a user presses the About menu command

---

[Back](README.md)
