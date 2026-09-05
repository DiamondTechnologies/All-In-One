# All In One

**All In One** is a Windows desktop application that combines source code files from a project into a single, structured document optimized for use with Large Language Models (LLMs).

Instead of manually copying dozens of files into ChatGPT, Claude, Gemini, or another LLM, All In One lets you select a project or a set of files and prepare their contents in a clean, LLM-friendly format.

## ✨ Features

* 📁 **Combine multiple files** into a single document
* 🧩 **Preserve project structure** with file names and clear separators
* 🤖 **LLM-friendly output** designed for LLM-assisted development
* 🚫 **Exclude unnecessary files** such as build artifacts, dependencies, and binaries
* 🎯 **Simple workflow** with no complicated configuration
* 🥸 **Remove or hide private data** such as comments, email addresses, and phone numbers

## Advantages

* 🏗️ **Well-built:** Uses the MVVM architecture for a clean and maintainable project structure
* 📦 **Packaged:** Runs in a sandboxed environment, providing an isolated application environment
* 🎨 **Modern:** Built with WinUI 3 and .NET 10, with support for the modern Windows 11 context menu
* 🛡️ **Memory-safe:** Uses memory-safe C# code whenever possible and relies on C++ only when native APIs or low-level functionality require it
* ⚡ **Native:** Built specifically for Windows to provide high performance and efficient resource usage

## Why All In One?

When working with an LLM on a software project, providing the right context is often the hardest part.

A typical workflow looks like this:

1. Open a file.
2. Copy its contents.
3. Switch to the LLM.
4. Paste it.
5. Repeat for every relevant file.
6. Try to explain how the files are connected.

All In One simplifies this process.

Just select the files, right-click and choose **All In One**, or use drag and drop.

The result is a single document containing the relevant project files.

## 🚀 Getting Started

### 1. Download & Install

Download the latest MSIX package from the **Releases** section.

Open the file and click **Install**.

### 2. Launch

Run All In One on your Windows machine.

### 3. Select Your Files

Choose the files or directories you want to provide as context.

You can include an entire project or only the files relevant to your current task.

### 4. Configure Exclusions

Use the tree view to exclude files that are not relevant to your task.

### 5. Generate the Context

Click **All In One** to combine the selected files into a single LLM-ready document.

### 6. Copy and Paste

Copy the generated content and paste it into your preferred LLM.

## 🛠️ Supported File Types

All In One is primarily designed for source code and text-based project files.

Examples include:

* Kotlin
* Java
* Python
* C / C++
* C#
* Ruby
* JavaScript / TypeScript
* HTML / CSS
* XML
* JSON
* YAML
* Markdown
* SQL
* Shell scripts
* Configuration files
* And many other text-based formats

## 🔒 Privacy

All In One processes your files locally on your computer.

Your project files are **not uploaded to an external server by All In One**.

The generated content only leaves your computer when you choose to copy it or share it with an external service such as an LLM provider.

> **Important:** Do not include passwords, API keys, private certificates, tokens, or other sensitive information in the generated context. Use the **Remove Sensitive Information** feature when needed.

## 🖥️ Requirements

* Windows 11
* Free local storage space: 500 MB minimum, 1 GB recommended

## Acknowledgements

* SignPath Foundation
* Microsoft Corporation

## License

	All In One is free software: you can redistribute it and/or modify
	it under the terms of the GNU Affero General Public License as published
	by the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
	GNU Affero General Public License for more details.

	You should have received a copy of the GNU Affero General Public License
	along with this program. If not, see https://www.gnu.org/licenses/.