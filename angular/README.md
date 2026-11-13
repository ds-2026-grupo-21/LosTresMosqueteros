# Frontend Angular

Interfaz de Despensa Inteligente con Angular 22 y ABP. Los requisitos, la configuración del backend y las funciones disponibles están en el [README principal](../README.md).

Desde este directorio:

```powershell
yarn install --frozen-lockfile
yarn start
```

Abrir [http://localhost:4200](http://localhost:4200), con el backend iniciado en `https://localhost:44312`. La configuración de desarrollo está en `src/environments/environment.ts`.

## Pruebas y build

```powershell
yarn test --watch=false
yarn build
```

Los tests de componentes usan Vitest. El CI ejecuta estos comandos con Node.js 24.

## Proxies y traducciones

Después de cambiar contratos del backend, regenerar los proxies con el host iniciado:

```powershell
abp generate-proxy -t ng --url https://localhost:44312
```

No editar manualmente `src/app/proxy`. Las traducciones propias se sirven desde el backend; reiniciarlo después de modificar sus archivos de localización.
