# Sistema de Gestión para Librería Bazar Rosita

**Curso:** Diseño y Arquitectura de Software  
**Grupo:** LOS-MIGAJEROS

## 1. Descripción

Proyecto académico que consiste en el desarrollo de un sistema web para gestionar las operaciones de Librería Bazar Rosita.

El sistema permite administrar productos, servicios, ventas, inventario, proveedores, pedidos y usuarios.

## 2. Tecnologías utilizadas

- Lenguaje de programación: C#
- Framework: ASP.NET Core MVC (.NET 8)
- Acceso a datos: Entity Framework Core
- Base de datos: Microsoft SQL Server
- Administración de base de datos: SQL Server Management Studio (SSMS)
- Entorno de desarrollo: Visual Studio 2022
- Modelado UML: Modelio
- Control de versiones: Git y GitHub

## 3. Funcionalidades

- Inicio y cierre de sesión.
- Gestión de productos y servicios.
- Registro de ventas y pagos.
- Consulta del historial de ventas.
- Generación de reportes y exportación CSV.
- Consulta y ajuste de inventario.
- Gestión de proveedores.
- Registro de pedidos, pagos y recepción de mercadería.
- Gestión de usuarios con roles Propietario y Encargado.

## 4. Estructura del repositorio

- **Base de Datos:** scripts SQL para crear y poblar la base de datos.
- **Codigo/LibreriaBazarRosita_2.0:** versión actual del sistema web ASP.NET Core MVC.
- **Codigo/codigo Antiguo:** versiones anteriores del proyecto.
- **Documentos:** documentación académica.
- **Modelio:** modelos y diagramas UML.
- **Pregunta 2 T2:** material correspondiente a la evaluación T2.

## 5. Requisitos para ejecutar el sistema

- Windows.
- Visual Studio 2022 con soporte para ASP.NET Core.
- SDK de .NET 8.
- SQL Server Express o una edición compatible.
- SQL Server Management Studio (SSMS).

## 6. Instalación

### Paso 1. Obtener el proyecto

Clonar el repositorio desde GitHub o descargarlo como archivo ZIP.

### Paso 2. Crear la base de datos

1. Abrir SQL Server Management Studio.
2. Conectarse al servidor SQL Server local.
3. Abrir el archivo `Base de Datos/LibreriaBazarRosita_Completa.sql`.
4. Ejecutar el script completo.
5. Verificar que se haya creado la base de datos `LibreriaBazarRosita`.

**Importante:** ejecutar el script completo sobre una instalación donde todavía no exista esta base de datos. No volver a ejecutarlo sobre una base que ya contenga información de trabajo sin revisar sus instrucciones.

### Paso 3. Abrir la solución

Abrir el archivo:

`Codigo/LibreriaBazarRosita_2.0/LibreriaBazarRosita.sln`

Esperar a que Visual Studio restaure los paquetes NuGet necesarios.

### Paso 4. Configurar la conexión a SQL Server

Abrir:

`Codigo/LibreriaBazarRosita_2.0/LibreriaBazarRosita.Web/appsettings.json`

Buscar `ConnectionStrings` y modificar únicamente el valor de `Server` por el nombre del servidor utilizado en SSMS.

Mantener el nombre de la base de datos:

`Database=LibreriaBazarRosita`

La configuración utiliza autenticación de Windows (`Trusted_Connection=True`).

### Paso 5. Ejecutar

1. Establecer `LibreriaBazarRosita.Web` como proyecto de inicio.
2. Compilar la solución.
3. Presionar **F5**.
4. Iniciar sesión con una cuenta habilitada en la base de datos.

Las credenciales de demostración deben comunicarse entre los integrantes del equipo y no publicarse en este repositorio.

## 7. Roles del sistema

**Propietario:** acceso a los módulos operativos y a la administración de usuarios.

**Encargado:** acceso a los módulos operativos autorizados, sin permisos para gestionar usuarios.

## 8. Actualización del repositorio

Para obtener los cambios publicados por el equipo, ejecutar desde la carpeta del repositorio:

`git pull origin main`

Antes de actualizar, guardar las modificaciones locales. No publicar conexiones personales ni contraseñas en GitHub.

## 9. Finalidad

Este sistema fue desarrollado con fines académicos para el curso de Diseño y Arquitectura de Software.