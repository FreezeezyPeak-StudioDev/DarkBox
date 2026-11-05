# Instrucciones del proyecto

## Al finalizar cada cambio

Incluir siempre al final 3 bloques separados, en texto plano y listos para copiar. Presentar cada bloque dentro de su propio bloque de código.

1. **Título del commit**: una sola línea en español, de 50 caracteres o menos. Debe resumir el cambio principal.
2. **Descripción del commit**: una sola línea en español, de 50 caracteres o menos. Debe resumir el alcance del cambio.
3. **Actualización**: lista de lo que se hizo y se agregó, redactada en pasado y con estas categorías cuando apliquen:
   - Implementación: funcionalidades o archivos nuevos.
   - Corrección: errores solucionados.
   - Limpieza: archivos, recursos o código eliminado.
   - Documentación: README, comentarios o configuración.
   - Verificación: compilación, pruebas o ejecución probada.
   - Exclusión: archivos que no se incluyeron.
   - Pendiente: lo que no se pudo verificar o queda por revisar.

Mencionar solo cambios reales y verificables. La sección **Actualización** no debe contener instrucciones para el usuario; esas van en la respuesta normal.

## Antes de cerrar un cambio

Revisar esta lista antes de dar el cambio por terminado:

- El cambio hace solo lo que se pidió, sin tocar archivos no relacionados.
- No quedaron errores de sintaxis, variables sin definir ni imports sin usar.
- Las funciones existentes siguen funcionando con sus parámetros y tipos originales.
- Se probó el caso normal y al menos un caso de error (entrada vacía, dato inválido, sin conexión).
- Se revisó la salida de consola o del navegador sin errores nuevos.
- Si algo no se pudo probar, se indica en **Pendiente**. Nunca marcar como verificado algo que no se ejecutó.

## Idioma

- Documentación, comentarios, mensajes al usuario y títulos de commit en español.
- Nombres de variables, funciones y archivos en inglés o español, pero de forma consistente dentro del proyecto.
- Usar inglés solo si el usuario lo pide explícitamente.

## Código limpio

- Preferir funciones pequeñas con una sola responsabilidad.
- Evitar código duplicado; extraer funciones auxiliares cuando un bloque se repita.
- No dejar código muerto, variables sin uso ni comentarios de depuración.
- Nombres descriptivos; evitar abreviaturas poco claras.
- Mantener el estilo existente del proyecto antes de introducir uno nuevo.
- No silenciar errores con bloques `catch` vacíos. Registrar el error o mostrar un mensaje útil.
- No agregar dependencias nuevas sin preguntar antes al usuario.

## Seguridad

- Nunca escribir claves, contraseñas, tokens ni URLs con credenciales en archivos del proyecto. Usar variables de entorno.
- Nunca registrar en consola ni en logs contraseñas, tokens, claves o datos personales.
- Validar y sanitizar cualquier entrada del usuario antes de usarla.
- Usar `textContent` en lugar de `innerHTML` cuando el contenido no sea HTML confiable.
- Usar consultas parametrizadas. Nunca armar SQL concatenando texto del usuario.
- Aplicar el principio de menor privilegio: cada función o cuenta solo tiene los permisos que necesita.
- No deshabilitar validaciones de seguridad para hacer pasar una prueba.
- Si un cambio toca autenticación, permisos o datos sensibles, indicarlo explícitamente al usuario.

## Calidad profesional

- Antes de cerrar un cambio, revisar que no rompa funciones existentes.
- Documentar en español las funciones nuevas que no sean evidentes, con una línea breve.
- Preferir cambios pequeños y reversibles frente a reescrituras grandes, salvo que el usuario las pida.
- Si una instrucción es ambigua o contradice este archivo, preguntar antes de actuar.
- Si el cambio es riesgoso (borrar datos, cambiar permisos, modificar una base de datos), explicar el riesgo y cómo revertirlo antes de aplicarlo.

## Datos y bases de datos (si aplica)

- Elegir el tipo de dato según lo que guarda, y definir un límite razonable para cada campo de texto.
- Validar los límites en la base de datos y no solo en el frontend.
- Los cambios de estructura en datos existentes se hacen con una migración nueva, no editando el esquema original.
- Probar cualquier cambio de datos en un entorno de pruebas antes de aplicarlo en producción.
- Tener copia de seguridad antes de ejecutar scripts que borren o recreen tablas.

## Publicación y control de versiones (completar por proyecto)

- Indicar aquí cómo se sube el proyecto (Git local, GitHub, carga manual, etc.).
- No hacer commit ni push salvo que el usuario lo pida explícitamente.
- Indicar aquí pasos especiales de despliegue, como limpiar caché o ejecutar un comando.