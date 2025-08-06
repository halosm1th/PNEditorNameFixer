# PNEditorNameFixer

**PNEditorNameFixer** is a utility for cleaning and standardizing personal name spellings in papyrological metadata—specifically fixing editor name variants in papyri.info (PN). It helps detect split author and editor tags to contain a surname and forename tag.

## 🧠 Features

* Reads PN xml records.
* Checks if the record has an author node, and if it does, check  if it has forename and surname nodes.
* If it does not have those, add them.
* Logs all changes for auditing.

## 📌 Requirements

* **.NET Core SDK** or **.NET 8+ runtime**
* Windows 10 / macOS 10.15 / Linux
* Terminal command‑line tool (`dotnet run`, etc.)

## 🚀 Quick Start

### Build & Run

```bash
git clone https://github.com/halosm1th/PNEditorNameFixer.git
cd PNEditorNameFixer
```

## 📁 Directory Structure


```bash
project-root/
├── IDP.data/
│   └── biblio
├── PNEditorNameFixer/
│   └── ...
```

## 🧪 Troubleshooting

* **Command not found?** Ensure `.NET SDK` is installed and on your path.

## 🤝 Contributing

Contributions are welcome! To help:

* Fork the repository.
* Create a new branch for your feature or bugfix.
* Add/update tests if applicable.
* Submit a pull request and ideally open an issue beforehand to discuss major changes.
