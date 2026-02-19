# Jellyfin Metadata Organiser Plugin

<div align="center">
    <p>
        <img alt="Logo" src="https://raw.githubusercontent.com/geo-martino/jellyfin-plugin-metadataorganiser/master/images/logo.png" height="350"/>
    <br>
        <a href="https://github.com/geo-martino/jellyfin-plugin-metadataorganiser/releases"><img alt="Total GitHub Downloads" src="https://img.shields.io/github/downloads/geo-martino/jellyfin-plugin-metadataorganiser/total"/></img></a>
        <a href="https://github.com/geo-martino/jellyfin-plugin-metadataorganiser/issues"><img alt="GitHub Issues or Pull Requests" src="https://img.shields.io/github/issues/geo-martino/jellyfin-plugin-metadataorganiser"/></img></a>
        <a href="https://github.com/geo-martino/jellyfin-plugin-metadataorganiser/releases"><img alt="Build and Release" src="https://github.com/geo-martino/jellyfin-plugin-metadataorganiser/actions/workflows/deploy.yml/badge.svg"/></img></a>
        <a href="https://jellyfin.org/"><img alt="Jellyfin Version" src="https://img.shields.io/badge/Jellyfin-10.11-blue.svg"/></img></a>
    <br>
        <a href="https://github.com/geo-martino/jellyfin-plugin-metadataorganiser"><img alt="Code Size" src="https://img.shields.io/github/languages/code-size/geo-martino/jellyfin-plugin-metadataorganiser?label=Code%20Size"/></img></a>
        <a href="https://github.com/geo-martino/jellyfin-plugin-metadataorganiser/graphs/contributors"><img alt="Contributors" src="https://img.shields.io/github/contributors/geo-martino/jellyfin-plugin-metadataorganiser?logo=github&label=Contributors"/></img></a>
        <a href="https://github.com/geo-martino/jellyfin-plugin-metadataorganiser/blob/master/LICENSE"><img alt="License" src="https://img.shields.io/github/license/geo-martino/jellyfin-plugin-metadataorganiser?label=License"/></img></a>
    </p>
</div>

This plugin automatically handles assignment of metadata to files and modifying metadata read from files. 

## ✨ Features

- **Dynamic Metadata Modification**: Embed Jellyfin metadata to directly to files on the filsystem.
- **Embedded Metadata Stripping**: Option to ignore or remove specific embedded metadata fields (like "Comment" or "Encoder").
- **Manual Metadata Remapping**: Option to provide an additional manual mapping file to remap embedded metadata as needed. 
- **Scheduled Metadata Refresh**: Integration with Jellyfin's Scheduled Tasks to periodically re-evaluate and "fix" metadata for newly added or changed items.

## Configuration

You may configure the plugin via the Jellyfin UI by going to the plugin's settings page. You will be able to configure from the options as shown below.

<div align="center">
    <p>
        <img alt="Configuration page 1" src="https://raw.githubusercontent.com/geo-martino/jellyfin-plugin-metadataorganiser/master/images/config_1.png" width="600"/>
        <img alt="Configuration page 2" src="https://raw.githubusercontent.com/geo-martino/jellyfin-plugin-metadataorganiser/master/images/config_2.png" width="600"/>
    </p>
</div>

## 📦 How to Install

1. Add this repository URL to your Jellyfin plugin catalog:
```
https://raw.githubusercontent.com/geo-martino/jellyfin-plugin-repository/master/manifest.json
```
2. Install the plugin
3. Restart Jellyfin

## 🤝 Contributing
Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.
