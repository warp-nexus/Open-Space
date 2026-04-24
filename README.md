<p align="center">
  <img alt="Space Station 14" width="600" src="Resources/Textures/Logo/logo.png" />
</p>

<div class="header" align="center">

[![Discord](https://img.shields.io/discord/1243455873989349448?style=for-the-badge&logo=discord&logoColor=white&label=Discord&color=%237289da)](https://discord.gg/NUCt8bm5JJ)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge)](https://dotnet.microsoft.com/)
[![Steam](https://img.shields.io/badge/Steam-SS14-1b2838?style=for-the-badge&logo=steam&logoColor=white)](https://store.steampowered.com/app/1255460/Space_Station_14/)
[![Client](https://img.shields.io/badge/Client-Download-0078D7?style=for-the-badge&logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHBhdGggZmlsbD0id2hpdGUiIGQ9Ik0xMiAxNmwtNS01aDMuNVY0aDNWMTFIMTd6TTUgMTh2Mmgxdjh6Ii8+PC9zdmc+)](https://spacestation14.io/about/nightlies/)
[![GitHub Stars](https://img.shields.io/github/stars/ss14-art/open-space?style=for-the-badge&logo=github&logoColor=white&color=181717)](https://github.com/ss14-art/open-space)

# Space Station 14

![Commit Activity](https://img.shields.io/github/commit-activity/y/ss14-art/open-space?style=for-the-badge&logo=github&logoColor=white&color=181717)
![Issues](https://img.shields.io/github/issues/ss14-art/open-space?style=for-the-badge&logo=github&logoColor=white&color=181717)
![Closed PRs](https://img.shields.io/github/issues-pr-closed/ss14-art/open-space?style=for-the-badge&logo=github&logoColor=white&color=181717)

</div>

---

## 🇷🇺 О проекте / 🇬🇧 About the project

**🇷🇺 Русский**
**Open Space** — это проект с открытым исходным кодом, нацеленный на создание уникальных механик и приятной игровой атмосферы в игре Space Station 14.
Это игра о выживании на космической станции, где происходят постоянные столкновения между экипажем и антагонистами, стремящимися помешать персоналу достичь своих целей.

**🇬🇧 English**
**Open Space** is an open-source project aimed at creating unique mechanics and a pleasant game atmosphere in Space Station 14.
It is a game about survival on a space station featuring constant confrontations between the crew and antagonists designed to prevent the crew from achieving their goals.

---

## 📚 Документация / Documentation

**🇷🇺** У Space Station 14 есть [сайт с документацией](https://docs.spacestation14.io/) по контенту, движку, геймдизайну и многому другому. Мы также предоставляем множество ресурсов для новых разработчиков.

**🇬🇧** Space Station 14 has a [docs site](https://docs.spacestation14.io/) containing documentation on SS14's content, engine, game design, and more. We also have lots of resources for new contributors to the project.

---

## 🚀 Запуск сборки локально / Building Locally

### Требования / Requirements

- **Git** — [скачать / download](https://git-scm.com/downloads)
- **.NET SDK 10.0 или выше / or higher** — [скачать / download](https://dotnet.microsoft.com/download/dotnet/10.0)

### 🍃 Windows
```bat
# 1. Клонируйте репозиторий
git clone https://github.com/ss14-art/open-space.git
cd open-space

# 2. Загрузите движок
git submodule update --init --recursive

# 3. Запустите сервер
runserver.bat

# 4. Запустите клиент (в отдельном окне)
runclient.bat
```

**Готово!** Подключитесь к **localhost** в клиенте и играйте.

> Для Release-сборки используйте `runserver-Release.bat` и `runclient-Release.bat`

### 🐧 Linux / macOS
```sh
# 1. Клонируйте репозиторий
git clone https://github.com/ss14-art/open-space.git
cd open-space

# 2. Загрузите движок
git submodule update --init --recursive

# 3. Запустите сервер
chmod +x runserver.sh
./runserver.sh

# 4. Запустите клиент (в отдельном терминале)
chmod +x runclient.sh
./runclient.sh
```

**Готово!** Подключитесь к **localhost** в клиенте и играйте.

> Для Release-сборки используйте `runserver-Release.sh` и `runclient-Release.sh`

---

## 🛠 Участие в разработке / Community & Contributing

**🇷🇺** Мы рады любой помощи в развитии проекта! Пожалуйста, ознакомьтесь с нашими правилами перед тем, как предлагать свои изменения.
**🇬🇧** We welcome any help in developing the project! Please read our guidelines before submitting your changes.

* 📖 [**Как внести вклад (Contributing)**](CONTRIBUTING.md) — Правила создания Pull Request'ов и требования к коду.
* ⚖️ [**Кодекс поведения (Code of Conduct)**](CODE_OF_CONDUCT.md) — Правила общения в нашем сообществе.
* 🛡️ [**Политика безопасности (Security Policy)**](SECURITY.md) — Как сообщить о критических уязвимостях и эксплойтах.

---

## ⚖️ Лицензия / License

> [!CAUTION]
> **🇷🇺 ВНИМАНИЕ:** Репозиторий использует комбинированную модель лицензирования.  
> Оригинальный код Space Wizards Federation распространяется под лицензией MIT.  
> Все изменения после коммита `efea656dd33f6296228a5d31be8ffc9f179f4f17` регулируются Project License, если в файле не указано иное.
>
> Некоторые файлы могут распространяться под другими лицензиями (например MPL 2.0), если это явно указано в начале файла через SPDX-заголовок.
>
> **🇬🇧 CAUTION:** This repository uses a combined licensing model.  
> The original Space Wizards Federation code is licensed under MIT.  
> All changes after commit `efea656dd33f6296228a5d31be8ffc9f179f4f17` are governed by the Project License unless stated otherwise.
>
> Some files may be licensed under different terms (e.g. MPL 2.0) if explicitly specified in the file header via SPDX.

---

### 📌 SPDX License Identification

> **🇷🇺** Лицензия конкретного файла может быть указана в его начале, например:
>
> ```csharp
> // SPDX-License-Identifier: MPL-2.0
> ```
>
> В этом случае файл распространяется по указанной лицензии, независимо от Project License.
>
> Если SPDX-заголовок отсутствует — применяется Project License (для кода после указанного коммита).
>
> **🇬🇧** A file may declare its license at the top using an SPDX header, for example:
>
> ```csharp
> // SPDX-License-Identifier: MPL-2.0
> ```
>
> In such cases, the file is governed by that license instead of the Project License.
>
> If no SPDX header is present — the Project License applies (for code after the specified commit).

---

### Нажмите на раздел для подробностей / Click each section for further information

<details>
<summary><b> Project License </b></summary>
<br>

![Project License](https://img.shields.io/badge/License-Project_License-blue?style=for-the-badge)

> **🇷🇺** Все изменения после коммита `efea656dd33f6296228a5d31be8ffc9f179f4f17`, для которых не указана иная лицензия (например через SPDX), регулируются условиями [LICENSE.TXT](LICENSE.TXT).  
> Коммерческое использование и несанкционированный публичный хостинг запрещены.
>
> **🇬🇧** All changes after commit `efea656dd33f6296228a5d31be8ffc9f179f4f17` that do not specify another license (e.g. via SPDX) are governed by [LICENSE.TXT](LICENSE.TXT).  
> Commercial use and unauthorized public hosting are prohibited.

</details>

<details>
<summary><b> CLA </b></summary>
<br>

![CLA](https://img.shields.io/badge/Agreement-CLA-orange?style=for-the-badge)

> **🇷🇺** Лицензионное соглашение контрибьютора. Создавая Pull Request, вы соглашаетесь с передачей прав на код организации ss14-art, как описано в [CLA.TXT](CLA.TXT).
>
> **🇬🇧** Contributor License Agreement. By submitting a Pull Request, you agree to transfer rights to the ss14-art organization as described in [CLA.TXT](CLA.TXT).

</details>

<details>
<summary><b> MIT License </b></summary>
<br>

![MIT License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

> **🇷🇺** Оригинальный код Space Wizards Federation, а также история коммитов до `efea656dd33f6296228a5d31be8ffc9f179f4f17`, распространяются по лицензии [MIT](MIT.TXT).
>
> **🇬🇧** The original Space Wizards Federation code and all commits prior to `efea656dd33f6296228a5d31be8ffc9f179f4f17` are licensed under [MIT](MIT.TXT).

</details>

<details>
<summary><b> MPL 2.0 License (per-file)</b></summary>
<br>

![MPL-2.0 License](https://img.shields.io/badge/License-MPL_2.0-yellow?style=for-the-badge)

> **🇷🇺** Некоторые файлы могут распространяться под лицензией MPL 2.0, если это указано в их начале через SPDX-заголовок.  
> В таком случае применяется лицензия MPL 2.0 вместо Project License.
>
> **🇬🇧** Some files may be licensed under MPL 2.0 if specified via an SPDX header at the top of the file.  
> In such cases, MPL 2.0 applies instead of the Project License.

</details>

<details>
<summary><b> CC 3.0 BY-SA </b></summary>
<br>

![Creative Commons 3.0 BY-SA](https://img.shields.io/badge/License-CC_3.0_BY--SA-lightblue?style=for-the-badge)

> **🇷🇺** Все ресурсы (Assets), не относящиеся к коду (иконки, звуки и т.д.), лицензированы по [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/), если не указано иное.
>
> **🇬🇧** All non-code assets (icons, sounds, etc.) are licensed under [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) unless stated otherwise.

</details>
