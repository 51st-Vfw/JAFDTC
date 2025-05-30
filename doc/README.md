# JAFDTC: User’s Guide

*Version 1.0.0-B.47 of 20-April-24*

_Just Another #%*@^!% DTC_ (JAFDTC) is a native Windows application that allows you to upload
data typically saved on a data cartridge in real life, such as steerpoints/waypoints and other
avionics setup, into a DCS module during a flight. There are three components to the user's
guide,

- This document provides a broad overview of JAFDTC
- The
  [_Common Elements Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md)
  discusses topics that are common across multiple airframes
- The
  [airframe guides](#what-now)
  present details that are specific to a particular airframe

General installation and troubleshooting instructions for JAFDTC can be found
[here](https://github.com/51st-Vfw/JAFDTC/tree/master/README.md)
and will not be covered in this document.

# Preliminaries

Before discussing the user interface, it is helpful to outline some of the key abstractions
that JAFDTC uses.

## Configurations & Systems

JAFDTC allows you to manage multiple avionics *Configurations* for multiple airframes.
Currently, JAFDTC supports A-10C Warthog, AV-8B Harrier, F-14A/B Tomcat, F-15E Strike Eagle,
F-16C Viper, F/A-18C Hornet, and Mirage M-2000C airframes.

### Overview

In JAFDTC, a *Configuration* is composed of multiple *System Configurations*, or *Systems*,
that each correspond to systems (strangely enough) in an airframe such as radios,
countermeasures, navigation, and so on. *Configurations* and *Systems* are **unique** to a
specific airframe, though different airframes may have systems that provide similar
functionality.

Each configuration has a unique name identifies the configuration in the user interface. This
name is set up when the configuration is first created and may be changed later.

> Configuration names must be unique across all configurations for an airframe and may contain
> any character. Names are case-insensitive so "A2G" and "a2g" are treated as the same name.

The specific systems available in a configuration, along with the system parameters that
JAFDTC can set up, vary from airframe to airframe.

> Details specific to a particular airframe can be found in the
> [airframe guides](#what-now)
> linked below.

Some systems may not exist in some airframes and even "common" systems may operate differently
in different airframes.

### Storing Configurations

JAFDTC stores configuration and supporting files, such as settings, in the `Documents\JAFDTC`
folder for the active profile. Configurations are found in the `Configs` folder in this
directory. Generally, you should not need to access the files in the `JAFDTC` folder as JAFDTC
supports sharing and exchanging information through various UI functions.

> As with all things, there are exceptions. A good general rule is if the JAFDTC UI can do
> something, use the UI and don't try to work around it.

### Avionics Defaults

A configurable parameter has a "default" state that corresponds to the state DCS models when
the jet comes out of a cold start in the absence of specific input by the pilot. Pilot
actions may change these values. Generally, JAFDTC has knowledge of default values for a
parameter, but may lack visibility into its "current" value.

Because of this, setting a parameter to a "default" value generally implies that JAFDTC will
not change the parameter from its *current* value in the avionics even if the current value
does not match the cold start value. Throughout this document, we will use "default" under
the assumption that the pilot has not changed parameters (so "default" and "current" are the
same value.

## Linking Systems

JAFDTC allows you to link *System Configurations* between different *Configurations* for the
same airframe. When linked, changes to the source system configuration are automatically
reflected in all linked systems.

> Links allow you to "compose" configurations from shared setups.

Links are tracked per system. That is, Systems X and Y in Configuration A can be linked
to those systems in a different configurations if desired. Once linked, changes to a system
are pushed to all linked systems regardless of whehter they are linked directly or
indirectly.

> For example, say System X in Configuration A is linked to Configuration B and System X in
> Configuration B is linked to Configuration C.
>
> Changes to System X in Configuration C will be reflected in System X in both Configraution
> A and B.

Breaking a link preserves the configuration of a linked system at the time the link is
broken. This is, if systems in Configuration A are linked to Configuration B and you delete
Configuration B, the linked systems in A will retain the values from B at the time it was
deleted.

> Links are **not** preserved across configuration exports and imports.

Linked systems are always edited in the source configuration. The system editors in linked
configurations will be read-only while the editor in the source configuration will allow
changes to be made.

Links are particularly useful when you have setups that you tend to reuse often. For example,
you might want to always configure your cockpit displays one way for A2G sorties and another
way for A2A sorties. Let's assume configurations for the airframe support an MFD system (MFD)
that sets up cockpit displays and a navigation system (NAV) that sets up steerpoints. Once
you setup your A2G and A2A MFD configurations, you can link them from new configurations to
avoid having to setup the MFD in each new configuration.

This pictures illustrates how this works,

![](images/Core_Cfg_Links.png)

Here, the arrow points to the source system configuration: the MFD system configuration in
"A2G Mission" comes from (or, is *linked to*) the MFD system configuration in "A2G Fav". In
this example, "A2G Mission" would only fully specify the set up for the NAV system; it relies
on the "A2G Fav" to specify the set up for the MFD system.

Any change you make to the MFD system in "A2G Fav" or "A2A Fav" is reflected in the
configurations that link to these system configurations; in this example, "A2G Mission",
"A2A Mission", and "Range A2A". Once linked, only the original is editable. That is, the A2G
MFD system will be read-only in "A2G Mission" but may be edited through "A2G Fav". In
general, changes to a system are pushed to all linked (either directly or indirectly) systems.

Though "A2A Fav" and "A2G Mission" have linked their MFD system configurations, they have
completely independent NAV system configurations as links are system-based.

Further, different systems can link to different configurations. In the above picutre,
"Range A2A" gets it's MFD setup from "A2A Fav" and its NAV setup from "KLAS STPTs". There
is no limit to the number of systems that can link to a particular system configuration.

Links are not affected by changes to configuration names: renaming "A2G Fav" to "A2G Base",
for example, will not break the link with "A2G Mission". In addition, settings are not lost
when you delete a source configuration. For example, though deleting "A2G Fav" will break the
link with "A2G Mission", the MFD settings in "A2G Mission" will match the MFD settings from
"A2G Fav" when the configuration was deleted.

## Points of Interest

JAFDTC supports a collection of points of interest (PoI) that can be used to speed up creation
of navigation points or target locations. There are three basic types of PoI,

- **DCS** &ndash; Includes airfields and other features defined on the supported DCS maps; for
  example, Nellis AFB from the NTTR map. These PoIs are provided by JAFDTC and cannot be edited
  by the user.
- **User** &ndash; Includes individual PoIs defined by the user; for example, a commonly used
  navigation point for a map. These PoIs are provided by, and can be edited by, the user.
- **Campaign** &ndash; Includes groups of PoIs defined by the user that support a group of
  missions; for example, a set of target DPIs for enemy industry in Beirut for a campaign
  set in Syria. These PoIs are managed by the user.

Each point of interest includes a name, location (in the form of a latitude, longitude, and
elevation), optional campaign information, and optional user-specified tag information that
classifies the point of interest.

The user interface provides mechanisms to search and select PoIs from the set of known
locations. Points of interest are discussed further
[later](#point-of-interest-database).

## DCS Integration

This section focuses on a brief overview of the integration between JAFDTC and DCS. The
[_Common Elements Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md)
covers the user interface aspects in more depth.

### DCS Support Scripts

To work with DCS, JAFDTC installs Lua within the `Scripts` hierarchy in the DCS installation(s)
present in the `Saved Games` folder associated with your profile. JAFDTC can install this
support in up to two places,

- `Saved Games\DCS\Scripts`
- `Saved Games\DCS.openbeta\Scripts`

depending which versions of DCS are installed on your system.

> As of the DCS 2.9.2.49940 release, the open beta and stable versions of DCS are the same
> though the folder names may still reflect the pre-2.9.2.49940 split between stable and
> open beta.

Within these areas, JAFDTC makes three changes,

- Adds scripts in the `Scripts\JAFDTC` folder to enable integration with supported airframes
- Adds `JAFDTCStatusMsgHook.lua` and `JAFDTCWyptCaptureHook.lua` script to the `Scripts\Hooks`
  folder to enables integration with DCS
- Adds a line to `Scripts\Export.lua` to load JAFDTC support into DCS at mission start

JAFDTC will automatically update these files as needed, notifying you when an update is made.

> If DCS is running when JAFDTC installs or updates the DCS support scripts, you should restart
> DCS to make sure DCS picks up the latest version of the DCS support scripts.

While JAFDTC allows you to decline the installation, doing so will prevent JAFDTC from
interacting with DCS in any capacity.

### Working with DCS DTC

ED delivered an initial DTC implementation in DCS 2.9.15.9408 released in April of 2025. While
the DTC implementation in DCS is not yet complete, it does provide some advantages over tools
like JAFDTC (primarily, by being able to inject configurations directly into the jet without
needing to rely on clicking cockpit controls). JAFDTC provides the ability to push
configurations into the jet through the ED DCS DTC, with some restrictions. The
[_Common Elements Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md#selecting--filtering-points-of-interest)
and
[airframe guides](#what-now)
provide further discussion on how JAFDTC interoperates with the DCS DTC.

Whether or not there is a place for tools like JAFDTC in the DCS ecosystem over the long
term reamins to be seen.

### Uploading Configurations to DCS

Once set up, a *Configuration* can be uploaded into the corresponding airframe in DCS through
the scripting engine that DCS exposes to the host system. To upload, JAFDTC walks through the
configuration, updating system parameters that differ from their defaults in the jet by
driving the clickable cockpit. For example, consider a BINGO warning system. If you change the
BINGO value from the default for the airframe, JAFDTC will update the BINGO value in the
avionics when uploading. If you do not change the value, JAFDTC will not make any changes to
that parameter in the airframe.

### Capturing Coordinates From DCS

JAFDTC can capture latitude, longitude, and elevation values from the DCS F10 map for use in a
system configuration (such as the location of a navigation point) as well as the point of
interest database.

# User Interface Basics

The JAFDTC user interface is based around a single window that displays a list of configrations
for an airframe and allows you to edit the specfic systems in a configuration. This section
covers the aspects of this user interface that are largely independent of the specific airframe
you are configuring. Additional details on user interface elements that are common to multiple
airframes can be found in the
[_Common Elements Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md)
along with the
[airframe guides](#what-now)
linked below.

## Configuration List Page

The main page of the JAFDTC user interface is the *Configuration List Page* that provides
a number of controls to manipulate configurations. This page is visible after launching JAFDTC.

![](images/Core_Cfg_List_Page.png)

Working from top to bottom, the primary components of this page include,

- [**Filter Field**](#filter-field)
  &ndash; Filters the configurations shown in the configuration list by name.
- [**Current Airframe**](#current-airframe-selection)
  &ndash; Selects which airframe to display known configurations for.
- [**Command Bar**](#command-bar)
  &ndash; Triggers commands to manipulate configurations.
- [**Configuration List**](#configuration-list)
  &ndash; Lists the available configurations for the current airframe.
- [**Status Area**](#status-area)
  &ndash; Shows information on the current DCS status and pilot.

The reaminder of this section discusses each of these elements in more detail.

### Filter Field

The filter field controls which configurations the
[configuration list](#configuration-list)
in the center of the page displays. To be displayed in the configuration list, a configuration
must match the filter by containing the specified text. For example, typing `test` will match
configurations that contain "test" anywhere in their name (comparisons are case-insensitive).

![](images/Core_Cfg_List_Filter.png)

As you type, the application will show a list of matching configurations. Typing `Return` or
clicking on the **Accept Filter** icon will select the filter. You can pick a specific
configuration by clicking on its name in the matching configuration list. Clicking on the `X`
**Clear Filter** icon will remove any filtering and display all configurations for the current
airframe.

### Current Airframe Selection

The combo box control in the upper right of the page selects the airframe currently in use. The
[configuration list](#configuration-list)
making up the bulk of the page displays known configurations for the selected airframe only.
JAFDTC remembers the last airframe you selected and will return to that airframe the next time
it is launched.

### Command Bar

The command bar at the top of the page provides quick access to the operations you can perform
on configurations. Clicking on the `...` button at the right of the bar displays the command bar
in its "open" state.

![](images/Core_Command_Bar.png)

The command bar includes the following commands,

- **Add** &ndash; Adds a new configuration to the database after prompting for a name for the new
  configuration.
- **Edit** &ndash; Navigates to the
  [System Editor Page](#system-editor-page)
  for the selected configuration to allow you to edit the configuration. You can also edit a
  configuration by double-clicking on the configuration in the configuration list.
- **Duplicate** &ndash; Creates a copy of the selected configuration after prompting for a name for the
  copy of the configuration.
- **Rename** &ndash; Renames the selected configuration.
- **Delete** &ndash; Removes the currently selected configuration from the database.
- **Import** &ndash; Creates a new configuration from a file previously created with the *Export*
  command.
- **Export** &ndash; Creates a file that contains the selected configuration suitable for import using
  the *Import* command.
- **Load to Jet** &ndash; Uploads the selected configuration to DCS, see
  [here](#interacting-with-dcs)
  for further details.
- **Focus DCS** &ndash; Brings DCS to the foreground and makes it the active application.

> Importing a configuration breaks any
> [links](#linking-systems)
> to other configurations that may have been in place at the time of export as
> [mentioned earlier](#linking-systems).
> The configuration will match the linked configuration at export, but will no longer update
> when changes are made to the source.

The overflow menu (exposed by clicking on the "`...`" button) holds three commands,

- **Points of Interest** &ndash; Navigates to the
  [POI Editor](#point-of-interest-database)
  page to allow you to edit points of interest.
- **Settings** &ndash; Opens up the
  [JAFDTC Settings](#settings)
  dialog to allow you to change JAFDTC settings.
- **About** &ndash; Opens a dialog box that identifies the JAFDTC version.

Depending on the state of the system, commands may be disabled. For example, **Edit** is disabled
when there is no configuration selected and **Load to Jet** is disabled if DCS is not running
a mission with the appropriate airframe.

### Configuration List

Configurations in the list are sorted alphabetically, with favorites appearing first. On the
left side of a row is the name of the configuration, a favorite icon (if the configuration is
marked as a favorite), and a brief summary of what systems the configuration updates. On the
right side of the row is a set of icons that also indicate which systems the configuration
modifies. Systems that are linked to other configurations are shown with a small gold dot in
the lower right corner. This list allows at most configuration to be selected at a time.

Double-clicking a row will open up the
[*System Editor Page*](#system-editor-page)
for the configuration that allows you to edit information in the configuration. Right-clicking
on a row will bring up a context menu with operations, such as **Rename** or **Delete**, that
you can perform on the clicked configuration. 

### Status Area

The status area occupies the bottom part of the configuration list page. On the right side of
this region is information showing the current status of DCS. There are three pieces of
status here, each marked with a red cross or green checkmark,

- **Lua Installed** &ndash; Indicates that the Lua support is properly installed in DCS.
- **Running** &ndash; Indicates that DCS is currently running (though not necessarily running
  a mission).
- **Pilot in Pit** &ndash; Indicates that DCS is currently running a mission along with the
  type of airframe currently in use.

For example,

![](images/Core_Cfg_DCS_Status.png)

shows two different status areas. On the left, DCS is not running but the Lua support is
installed. On the right, DCS is running a mission where the player is piloting an F-16C
Viper.

The left side of the status area identifies the pilot and wing as specified through the JAFDTC
[Settings](#settings).

## System Editor Page

The *System Editor Page* provides a list of systems from which you can display per-system
editors. The specific systems availble in a configuration vary from airframe to airframe.
However, the basic structure of the page on which you edit system configurations is similar.

![](images/Core_Cfg_Edit_Page.png)

At the top of the window is the name of the current configuration being edited along with a
back button that returns you to the
[*Configuration List Page*](#configuration-list-page)
when clicked. To the right is text identifying the *Current Airframe*.

### System List

The *System List* provides the systems that make up the configuration. Each system has an
associated icon whose tint and badging specifies details on the configuration.

![](images/Core_Base_Edit_Icons.png)

The tint of the icon indicates the state of the system: blue icons mark systems whose
configuration has changed from defaults, white icons mark systems that have not been changed.

> JAFDTC uses the system highlight color; if you change it through Windows settings, the blue
> icons in the screenshots in this guide may be a different color based on your choice.

A small gold dot in the lower right corner of the icon marks those systems that are linked to
other configurations.

> A white icon with a gold dot indicates a system that is linked to another configuration
> in which the system has not been changed from defaults.

Clicking on a row in this list changes the system editor panel to the right to a panel
appropriate for editing the selected system.

### System Editor

The bulk of the page is taken up by the system editor panel on the right. The content of this
panel depends on the system selected from the *System List* to the left. In the figure above,
the editor is showing the steerpoint list associated with the selected steerpoints system. See
the
[airframe guides](#what-now)
for further details on system editors for a particular airframe.

### Common Editor Controls

Depending on the system, the bottom edge of the system configuration editor may contain link
and reset buttons that provide common link and reset functions for systems.

* **Reset** &ndash; Restores the default settings to the system. This is disabled if the system
  is in its default configruation.
* **Link** &ndash; Links or unlinks the system to or from another configuration (see the
  [earlier discussion](#linking-systems)).

The *Link* button changes based on whether or not the system is linked,

![](images/Core_Cfg_Edit_Link.png)

When unlinked, the button displays "Link To". Clicking the button brings up a list of
potential source configurations the system can be linked to.

> In the
> [earlier example](#linking-systems),
> to link the MFD configuration in "A2G Fav" to "A2G Mission" you would click the "Link To"
> button in the MFD system editor in "A2G Mission" and select "A2G Fav" from the list of
> configurations to link to.

Once linked, edits to the system configuration are disabled (as you must edit the system
configuration through the source configuration) and the button changes to "Unlink From" with
the name of the configuration the system is linked to. When unlinking, the system configuration
does not change, but will no longer receive updates from the source configuration. Icons for
linked systems are badged with a small gold dot as described earlier.

## Point of Interest Database

JAFDTC provides a *Points of Interest* (PoI) database that contains common locations throughout
DCS theaters. This database consists of three types of entries as
[described earlier](#points-of-interest) and is generally used to make it easy to proivde
location information to
[navigation system editors](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md#navigation-system-editors)
and other location-aware systems.

Generally, DCS and User PoIs are treated as independent locations in the world. These points
are intended to support usage models like tracking an often-used airfield or approach fix to
allow the points to be quickly loaded into a steerpoint list.

Campaign PoIs differ in that they encompass a set of locations relevant to a mission or set of
missions. These PoI sets might include target DPIs for various targets or ingress points for a
mission. While DCS and User PoIs are independent, Campaign PoIs are intended to be managed as
a set.

For example, consider a campaign covering multiple missions. Prior to campaign start, you can
build campaign PoIs that include DPIs for all potential targets, ingress and egress
locations, approach fixes for homeplate and alternates, and so on. These can be used in
planning or in a mission in real time to set up pre-planned strike locations, CAP lanes, etc.
As the campaign progresses and targets are destoryed, or new targets become available, the
campaign PoIs can be updated and redistributed.

The **Point of Interest** command in the
[overflow menu](#command-bar)
opens up an editor page to manage known points of interest.

![](images/Core_Base_PoI.png)

The top portion of this page contains controls to filter the PoIs listed in the PoI list along
with a command bar control. Below these controls is a list of PoIs, one per row. The bottom of
the page provides controls to add and update *user* points of interest.

### Point of Interest List

The **Point of Interest List** in the center of the page lists known points of interest in the
database as filtered by the
[point of interet filter](#poiunt-of-interest-filter).
Each row in the **Point of Interest List** corresponds to a single PoI in the database and
provides information such as name and position (latitude, longitude, elevation). The icon at
the left of each row in this list indicates the PoI type:

- **Pin** &ndash; A user PoI.
- **Flag** &ndash; A campaign PoI.
- **No Icon** &ndash; A DCS system PoI.

The gray text in each row identifies the campaign the PoI is associated with along with
tags associated with the PoI.

You can select PoIs from the table using the standard Windows table interactions such as
`SHIFT`-click to extend the selection, and so on.

### Point of Interest Filter and Command Bar

The **Point of Interest Filter** controls at the top left of the window allow you to filter
the points of interest listed in the
[**Point of Interest List**](#point-of-interst-list).
These controls operate as described in the
[_Common Elements Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md#selecting--filtering-points-of-interest).

The command bar,

![](images/Core_Base_PoI_Cmd.png)

includes the following commands,

- **Edit** &ndash; Copies the properties from the selected PoI to the PoI editor.
- **Duplicate to User** &ndash; Copies the properties from the selected PoI to the PoI editor
  to create a new user PoI.
- **Copy to Campaign** &ndash; Copies the selected PoI(s) to a campaign.
- **Delete** &ndash; Deletes the selected PoI(s) from the database. Note that DCS PoIs cannot
  be deleted and deleting all campaign PoIs implicitly deletes the campaign.
- **Import** &ndash; Imports new PoIs from a previously exported file.
- **Export** &ndash; Exports selected PoIs to a file.

The overflow menu (exposed by clicking on the "`...`" button) holds three commands,

- **Add Campaign** &ndash; Creates a new campaign.
- **Delete Campaign** &ndash; Deletes an existing campaign and all associated PoIs.
- **Coordiante Format** &ndash; Selects the format (e.g., DMS, DDM) to use to display PoI
  coordiantes.

Depending on the state of the system, commands may be disabled. For example, **Edit** is
disabled when the selected PoI cannot be edited.

### Editing Points of Interest

The controls at the bottom of the page allow you to add or update user PoIs. Depending on the
PoI's type, double-clicking on PoI in the list, or selecting a PoI and clicking on the **Edit**
or **Duplicate to User** commands will populate the fields (name, tags, latitude, etc.) with
the values from the PoI.

> DCS and Campaign PoIs are read-only and cannot be edited. When editing a PoI of these types,
> JAFDTC creates a User PoI copy of the point of interest and edits that.

Based on context, the **Add** or **Update** button either adds a new User PoI to the database
(if the PoI has a unique name) or updates an existing User PoI in the database.

> Using the **Copy to Campaign** command will add a copy of an existing PoI (User or DCS) to
> a campaign.

The **Clear**
button clears the editor fields. Note that JAFDTC expects the PoI name to be unique within a
type, campaign, and theater.

### Exporting Points of Interest

Using the **Export** command from the
[**Command Bar**](#point-of-interest-filter-and-command-bar)
lets you export selected PoIs from the database in an internal `JSON` based format. You can
export all PoIs from a campaign by selecting one PoI from the campaign and selecting
**Export**. From this point, JAFDTC will display a standard Windows file picker to allow
you to specify the file to export to.

Exporting generally preserves the type and campaign of the exported PoIs. DCS PoIs are always
converted to User PoIs prior to export.

### Importing Points of Interest

Using the **Import** command from the
[**Command Bar**](#point-of-interest-filter-and-command-bar)
lets you export selected PoIs from the database in an internal `JSON` or `CSV` based formats.

> The `CSV` format is intended primarily for use in creating PoI lists for campaigns and
> so on from other source material such as a spreadsheet.

After selecting **Import**, JAFDTC will display a standard Windows file picker to allow you
to specify the file to import from. The import process will update PoIs that are in the
imported data as well as the current database. When importing a campaign that is also
currently in the database, the user can select merging the imported data or replacing the
in-database campaign data. Campaigns that are not in the database are implicitly created.

The `CSV` format is a text file of lines, one per PoI, of comma-separated fields. The format
of the fields is as follows,

```
[type],[campaign],[name],[tags],[latitude],[longitude],[elevation]
```

Where `[type]` is the integer 1 (User) or 2 (Campaign), `[campaign]` and `[name]` are strings,
`[tags]` is a semicolon-separated list of tags like "`Airbase;Target;Required`", `[latitude]`
and `[longitude]` are in decimal degrees positions, and `[elevation]` is an integer in feet.

## Settings

You can access the JAFDTC settings through the Settings command on the command bar overflow
menu as
[described earlier](#configuration-list-page).
The settings dialog box appears as follows,

![](images/Core_Settings.png)

There are multiple controls in the settings,

- **Wing Name**, **Callsign** &ndash; Specifies your wing and callsign. This information
  appears in the
  [status area](#status-area)
  of the
  [configuration list page](#configuration-list-page).
  Some airframes also use this inforamtion for configuration.
- **Upload Feedback** &ndash; Selects the type of feedback to provide during uploads,
  - *Audio Only* &ndash; Audio cues at the start and completion of upload.
  - *Lights Test Only* &ndash; Briefly triggers the "lamp test" function (on some select
     airframes).
  - *Audio &amp; Done Message* &ndash; Audio cues and an on-screen message in DCS indicating
    the upload has finished.
  - *Audio &amp; Progress Messages* &ndash; Audio cues and an on-screen message in DCS
    indicating the progress of the upload.
  - *Audio &amp; Light Test* &ndash; Audio cues and brief trigger of the "lamp test"
    function (on some airframes).
- **Navpoint Import Ignores Airframe** &ndash; When selected, importing navpoints from a
  [file](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md#importing-and-exporting-navigation-points)
  will not require the airframe in the file to match the airframe of the configuration.
- **JAFDTC Window Remains on Top** &ndash; Selects whether JAFDTC will always remain on
  top of the window stack, even while DCS has focus. This allows you to keep the DCS UI
  visible in non-VR operation.
- **Check for New Versions at Launch** &ndash; Selects whether JAFDTC will check if a new
  version is available each time it is launched.
- **Install DCS Lua Support** &ndash; Installs
  [DCS Lua support](#dcs-support-scripts)
  if the support is not currently installed (the button is disabled if support is installed).
- **Uninstall DCS Lua Support** &ndash; Uninstalls
  [DCS Lua support](#dcs-support-scripts)
  if the support is currently installed (the button is disabled if support is not installed).

JAFDTC saves its settings to a file in `Documents\JAFDTC`. Clicking “**OK**” will accept any
changes in the dialog, while “**Cancel**” will discard any changes.

# What Now?

Now that you have a basic familiarity with JAFDTC, you can take a look at the
[_Common Elements Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md)
that provides the next level of detail on JAFDTC, its operation, and its user interface. From
there, move on to the airframe guides for airframes of interest,

| Airframe | Systems JAFDTC Can Configure |
|:--------:|------------------------------|
| [A-10C Warthog](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_A10C.md) | DSMS, HMCS, IFFCC, Radios, TAD, TGP, Waypoints, Miscellaneous Systems
| [AV-8B Harrier](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_AV8B.md) | Waypoints
| [F-14A/B Tomcat](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_F14AB.md) | Waypoints
| [F-15E Strike Eagle](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_F15E.md) | MPD/MPCD Formats, Radios, Steerpoints, Miscellaneous Systems
| [F-16C Viper](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_F16C.md) | Countermeasures, Datalink, HARM (ALIC, HTS), MFD Formats, Radios, SMS Munitions, Steerpoints, Miscellaneous Systems
| [F/A-18C Hornet](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_FA18C.md) | Countermeasures, Pre-Planned Weapons, Radios, Waypoints
| [Mirage M-2000C](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_M2000C.md) | Waypoints

The above links provide additional details on JAFDTC's capabilities and operation on a specific
airframe.
