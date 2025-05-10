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
  File "TempViewerV1.1.exe"
  File "TempViewerV1.1.exe.config"

  # Создание ярлыка на рабочем столе
  CreateShortcut "$DESKTOP\TempViewer.lnk" "$INSTDIR\TempViewerV1.1.exe"

SectionEnd

# Секция удаления
Section "Uninstall"

  # Удаление файлов
  Delete "$INSTDIR\TempViewerV1.1.exe"
  Delete "$INSTDIR\TempViewerV1.1.exe.config"

  # Удаление ярлыка
  Delete "$DESKTOP\TempViewer.lnk"

  # Удаление папки
  RMDir "$INSTDIR"

SectionEnd
