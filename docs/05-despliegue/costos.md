# Control de costos

Cuenta nueva de AWS con el plan Free. Según la documentación del plan, da créditos (hasta $200 entre bienvenida y actividades, con vigencia de 6 meses) y la cuenta se cierra al agotarlos o al vencer el plazo, en lugar de cobrar. **Verificar en la consola** las condiciones vigentes y qué servicios cubre; si se pasa a un plan de pago, no hay tope automático. Los presupuestos solo avisan y los datos de costo tardan horas en reflejarse.

## Qué genera costo en este diseño
- Tareas de Fargate (vCPU y memoria por hora).
- El balanceador de carga de Express Mode (cargo por hora mientras exista) y las direcciones IPv4 públicas.
- Registros en CloudWatch Logs.
- Almacenamiento de imágenes en ECR.

## Controles
| Control | Cómo |
|---|---|
| Presupuesto | AWS Budgets: $5 al mes, alertas al 50, 90 y 100 % |
| Cómputo acotado | `MaxTaskCount = 2` y 0.25 vCPU / 0.5 GiB por tarea (parámetros de `service.yaml`) |
| Imágenes acotadas | La política de ciclo de vida de ECR conserva las últimas 10 |
| Sin tráfico | No hay escalado a cero: borrar el servicio con `./scripts/cleanup-aws.sh service` |
| Limpieza total | `./scripts/cleanup-aws.sh all` |
| Sin recursos extra | No crear nada fuera de `infra/aws/` (bases de datos, NAT gateways, instancias) |

## Límites de este enfoque
- Los presupuestos no detienen el gasto: revisar Billing tras el demo.
- Los grupos de logs creados por Express Mode pueden quedar tras borrar el stack: revisarlos y borrarlos.
- No hay cifras de costo verificadas aquí: usar AWS Pricing Calculator con la región elegida.

## Checklist
- [ ] Presupuesto creado antes de desplegar.
- [ ] Servicios cubiertos por el plan Free confirmados en la consola.
- [ ] Servicio borrado (o todo el entorno) después del demo.
- [ ] Sin balanceadores, grupos de logs ni imágenes olvidados.
