# Kite

Kite — учебный императивный язык со статической типизацией для курса "Теория автоматов и формальных языков". В первой итерации реализован лексический анализ файлов `.kite`. Лексер распознаёт токены и сообщает о лексических ошибках с номером строки и столбца.

Проект использует C# 14 и [SDK .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

## Клонирование проекта

Работа над первой итерацией ведётся в ветке `iteration_1`.

```bash
git clone --branch iteration_1 https://github.com/FannDrann/Kite.git
cd Kite
```

## Сборка

Запускайте команды из корня проекта.

```bash
dotnet build

dotnet test
```

## Анализ покрытия кода тестами

Перед первым запуском восстановите локальные инструменты.

```bash
dotnet tool restore --tool-manifest dotnet-tools.json
```

HTML-отчёт появится в файле `tests/coverage-report/index.html`.

### Bash

```bash
bash scripts/run-tests-with-coverage
```

### PowerShell

```powershell
./scripts/run-tests-with-coverage.ps1
```

## Структура проекта

| Путь | Назначение |
| --- | --- |
| [`Kite.slnx`](Kite.slnx) | Решение .NET с лексером и тестовым проектом |
| [`src/Kite.Lexing/`](src/Kite.Lexing) | Библиотека лексического анализа |
| [`tests/Kite.Lexing.UnitTests/`](tests/Kite.Lexing.UnitTests) | Модульные тесты xUnit |
| [`docs/specification.md`](docs/specification.md) | Спецификация языка Kite |
| [`examples/max.kite`](examples/max.kite) | Пример программы, который читает лексер в одном из тестов |

## Статический анализ

Анализаторы из шаблона курса запускаются при сборке. Общий файл [`Directory.Build.props`](Directory.Build.props) подключает .NET Analyzers, Roslynator, StyleCop и анализаторы xUnit. В [`.editorconfig`](.editorconfig) заданы правила форматирования и уровни диагностик.
