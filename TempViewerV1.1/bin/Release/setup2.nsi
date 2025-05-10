# Имя выходного файла установщика
Outfile "TempViewerSetup.exe"

# Путь установки (по умолчанию Program Files)
InstallDir "$PROGRAMFILES\TempViewer"

# Уровень прав администратора (если нужно писать в Program Files)
RequestExecutionLevel admin

# Основная секция установки
Section "Install"

  # Создание папки назначения
  SetOutPath "$INSTDIR"

  # Копирование файлов
  File "TempViewer.exe"
  File "TempViewer.exe.config"

  # Создание ярлыка на рабочем столе
  CreateShortcut "$DESKTOP\TempViewer.lnk" "$INSTDIR\TempViewer.exe"

  # Добавление команды для создания файла удаления
  WriteUninstaller "$INSTDIR\Uninstall.exe"

SectionEnd

# Секция удаления
Section "Uninstall"

  # Удаление файлов
  Delete "$INSTDIR\TempViewer.exe"
  Delete "$INSTDIR\TempViewer.exe.config"

  # Удаление ярлыка
  Delete "$DESKTOP\TempViewer.lnk"

  # Удаление папки
  RMDir "$INSTDIR"

SectionEnd
