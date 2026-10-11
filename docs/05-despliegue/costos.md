# Control de costos

Cuenta nueva de AWS con el plan Free. Según la documentación del plan, da créditos (hasta $200 entre bienvenida y actividades, con vigencia de 6 meses) y la cuenta se cierra al agotarlos o al vencer el plazo, en lugar de cobrar. **Verificar en la consola** las condiciones vigentes y qué servicios cubre; si se pasa a un plan de pago, no hay tope automático. Los presupuestos solo avisan y los datos de costo tardan horas en reflejarse.

## Estado verificado (2026-10-10)
- El plan Free de la cuenta estaba activo con $100 de crédito restante y vencimiento el 2027-04-09.
- El presupuesto `habit-tracker-5usd` ($5 al mes, alertas al 50, 90 y 100 %) está creado.
- Se aplicaron 20 USD de crédito por usar RDS y otros 20 USD por usar AWS Budgets (confirmado por el dueño en la consola, «Explore AWS»). Según la API de AWS, quedan 139.94 USD de crédito (antes 100 USD), con vencimiento el 2027-04-09.
- El servicio y la base de datos se desplegaron y se borraron tras las pruebas; existieron el tiempo que duró cada prueba. No se ha verificado el costo real: revisar Billing.
- Precios de lista de RDS en `us-east-2` (API de precios de AWS, 2026-10-10): 0.016 USD por hora la instancia `db.t4g.micro` Single-AZ y 0.115 USD por GB-mes el almacenamiento gp3. Con 20 GiB son unos 2.30 USD al mes, y la base completa cuesta del orden de 0.02 USD por hora (unos 0.5 USD al día) aunque no tenga tráfico. Son precios de lista, no el gasto real.

## Qué genera costo en este diseño
- La base de datos RDS (instancia por hora y almacenamiento), que cobra mientras exista, tenga o no tráfico.
- El secreto de Secrets Manager que administra RDS (cargo mensual no verificado).
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
| Base de datos acotada | `db.t4g.micro`, Single-AZ y 20 GiB (parámetros de `db.yaml`) |
| Sin tráfico | No hay escalado a cero: borrar el servicio con `./scripts/cleanup-aws.sh service` (la base sigue cobrando) |
| Borrar servicio y base | `./scripts/cleanup-aws.sh db` (la base se borra sin instantánea: los datos se pierden) |
| Limpieza total | `./scripts/cleanup-aws.sh all` |
| Sin recursos extra | No crear nada fuera de `infra/aws/` (NAT gateways, instancias, réplicas ni Multi-AZ) |

## Límites de este enfoque
- Los presupuestos no detienen el gasto: revisar Billing tras el demo.
- Los grupos de logs creados por Express Mode (uno por despliegue) y las revisiones de la definición de tarea quedan tras borrar el stack: no tienen costo relevante, pero hay que borrarlos a mano.
- Cada despliegue recrea el servicio y el balanceador y vuelve a cobrar por hora hasta borrarlos. La base de datos cobra desde que se crea con `setup-db.sh` hasta que se borra.
- No hay cifras de gasto verificadas para Fargate ni el balanceador: usar AWS Pricing Calculator con la región elegida y revisar Billing.

## Checklist
- [ ] Presupuesto creado antes de desplegar.
- [ ] Servicios cubiertos por el plan Free confirmados en la consola.
- [ ] Servicio y base de datos borrados (o todo el entorno) después del demo.
- [ ] Sin balanceadores, instancias RDS, secretos, grupos de logs ni imágenes olvidados.
