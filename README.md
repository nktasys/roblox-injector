# MizaKusense - Roblox FFlags Injector

## Descripción

MizaKusense es un launcher de inyección de FFlags para Roblox escrito en C# (.NET 8.0) con una interfaz gráfica completa de Windows Forms.

## Características

- ✅ **UI completa con interfaz gráfica**
- ✅ **Botón INJECT** - Inyecta los FFlags cargados en el proceso de Roblox
- ✅ **Botón GITHUB OFFSETS** - Carga automáticamente los offsets desde GitHub
- ✅ **Botón IMPORT JSON** - Importa FFlags desde archivos JSON o pega JSON manualmente
- ✅ **Indicador de PROCESOS** - Muestra si Roblox está conectado o no
- ✅ **Detección automática de Roblox** - Verifica si el proceso RobloxPlayerBeta está ejecutándose
- ✅ **Auto-actualización de offsets** - Se actualiza cada 30 segundos desde GitHub
- ✅ **Hotkey personalizable** - Por defecto Shift+F5 para mostrar/ocultar la ventana
- ✅ **Modo Stealth** - Anti-screenshare y anti-recording
- ✅ **Auto-inyección** - Re-inyecta automáticamente si los flags se revierten
- ✅ **No se cierra al cerrar** - Al cerrar la ventana, solo se oculta y sigue ejecutándose en segundo plano
- ✅ **0 Warnings, 0 Errores** - Código limpio y optimizado
- ✅ **Intervalo de inyección** - Inyecta automáticamente a intervalos configurables

## Requisitos

- Windows 10/11
- .NET 8.0 Runtime o SDK
- Roblox instalado

## Instalación

1. Asegúrate de tener .NET 8.0 instalado:
   ```bash
   dotnet --version
   ```

2. Compilar el proyecto:
   ```bash
   dotnet build MizaKusense.csproj
   ```

3. Ejecutar la aplicación:
   ```bash
   dotnet run
   ```
   
   O ejecutar directamente el archivo compilado:
   ```
   bin\Debug\net8.0-windows\MizaKusense.exe
   ```
   
   O usar el script de reconstrucción:
   ```
   rebuild.bat
   ```

## Uso

### 1. Cargar FFlags

**Opción A: Cargar desde GitHub (Recomendado)**
- Click en el botón **"GITHUB OFFSETS"**
- La aplicación descargará automáticamente los offsets más recientes

**Opción B: Importar JSON**
- Click en el botón **"IMPORT JSON"**
- Pega tu JSON de FFlags o importa desde un archivo
- Ejemplo de JSON:
  ```json
  {
    "FFlagDebugDisplayFPS": true,
    "FFlagDebugGraphicsEnableJobThrottling": false,
    "DFIntDebugRenderFPSLimit": 999
  }
  ```

### 2. Inyectar FFlags

1. Asegúrate de que Roblox esté ejecutándose
2. Click en el botón **"INJECT"**
3. Los flags se inyectarán en el proceso de Roblox

### 3. Verificar Estado

En la parte superior de la UI verás:
- **FLAGS**: Número de flags cargados
- **OFFSETS**: Número de offsets disponibles
- **PROCESOS**: Número de procesos de Roblox activos (0 = desconectado, 1+ = conectado)

### 4. Características Avanzadas

**Auto-reinyección:**
- Habilita "Auto re-inject on revert" para re-inyectar automáticamente si Roblox revierte los flags

**Inyección por intervalo:**
- Habilita "Auto-inject interval" y ajusta el slider (100ms - 10000ms)

**Modo Stealth:**
- Habilita "Stealth mode (anti-screenshare)" para ocultar la ventana de capturas de pantalla

**Hotkey:**
- Click en "CHANGE HOTKEY" para cambiar el atajo de teclado
- Por defecto: Shift+F5

## Archivos del Proyecto

- [`MizaKusense.csproj`](MizaKusense.csproj) - Archivo de proyecto
- [`Program.cs`](Program.cs) - Punto de entrada de la aplicación
- [`MizaKusenseForm.cs`](MizaKusenseForm.cs) - Interfaz gráfica principal
- [`Injector.cs`](Injector.cs) - Lógica de inyección de memoria
- [`injection.txt`](injection.txt) - Archivo original (para referencia)

## Estructura de la UI

```
┌─────────────────────────────────────────────────┐
│ MIZAKUSENSE                    [×]         │
├─────────────────────────────────────────────────┤
│ FLAGS: X    OFFSETS: Y    PROCESOS: Z    │
├──────────────────────┬──────────────────────┤
│ INJECTION METHOD    │ ACTIVITY LOG          │
│ [Offsets ▼]        │ [CLEAR]              │
│                     │                      │
│ ANTI-FEATURES      │ [log messages...]    │
│ ☑ Stealth write    │                      │
│ ☑ Signature enc.   │                      │
│ ☑ Randomization     │                      │
│ ☐ Timing attack     │                      │
│ ☐ Auto re-inject    │                      │
│ ☐ Auto-inject int.  │                      │
│ [slider] [1000] ms  │                      │
│                     │                      │
│ STEALTH             │ LOAD OFFSETS         │
│ ☑ Hide taskbar      │ [GITHUB OFFSETS]     │
│ ☐ Disable screenshots│ [IMPORT JSON]         │
│ ☐ Stealth mode      │                      │
│ ☑ Safe mode         │ Auto-updates from     │
│                     │ GitHub every 30s      │
│                     │ Offsets loaded: X      │
│                     │                      │
│ [CHANGE HOTKEY]      │                      │
│ [INJECT] [UNINJECT]│                     │
│ [CLOSE]              │                     │
└──────────────────────┴──────────────────────┘
```

## Solución de Problemas

**Error: "Roblox process not found!"**
- Asegúrate de que Roblox esté ejecutándose
- Verifica que el proceso se llame "RobloxPlayerBeta"

**Error: "Failed to load from GitHub"**
- Verifica tu conexión a internet
- Intenta cargar los offsets manualmente

**La ventana no aparece**
- Presiona Shift+F5 (o tu hotkey configurado) para mostrar/ocultar la ventana

## Notas

- La aplicación se conecta a GitHub automáticamente cada 30 segundos para actualizar los offsets
- Los offsets se obtienen de: https://github.com/NtReadVirtualMemory/Roblox-Offsets-Website
- La inyección usa WriteProcessMemory para modificar la memoria de Roblox
- Usa el modo "Safe mode" para evitar detección


