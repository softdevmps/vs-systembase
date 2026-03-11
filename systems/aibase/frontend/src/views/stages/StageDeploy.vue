<template>
  <v-container fluid class="stage-page">
    <v-card class="card sb-page-header">
      <div class="d-flex align-center justify-space-between flex-wrap ga-3">
        <div class="d-flex align-center">
          <div class="sb-page-icon"><v-icon color="primary" size="24">mdi-rocket-launch</v-icon></div>
          <div>
            <h2>Etapa 7 · Deploy</h2>
            <p class="sb-page-subtitle">Publica el modelo entrenado como servicio y valida que quede operativo.</p>
          </div>
        </div>
        <div class="d-flex align-center ga-2 flex-wrap">
          <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-arrow-left" @click="router.push('/stage/eval')">Volver</v-btn>
          <v-btn class="sb-btn primary" prepend-icon="mdi-arrow-right" :disabled="!selectedProjectId" @click="goNext">Ir a Playground</v-btn>
        </div>
      </div>
    </v-card>

    <v-alert
      v-if="error"
      class="mt-4"
      type="warning"
      variant="tonal"
      border="start"
      density="comfortable"
      :text="error"
    />

    <OptionGuide class="mt-4" :items="optionGuideItems" />

    <v-row class="mt-4" dense>
      <v-col cols="12" lg="7" xl="8" class="deploy-main-col">
        <v-card class="card">
          <v-card-title>Configuración de despliegue</v-card-title>
          <v-divider />
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="8">
                <v-select
                  v-model="selectedProjectId"
                  :items="projectItems"
                  item-title="title"
                  item-value="value"
                  label="Proyecto"
                  density="comfortable"
                  :loading="loadingData"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-select
                  v-model="deployForm.targetEnv"
                  :items="targetEnvOptions"
                  item-title="title"
                  item-value="value"
                  label="Ambiente"
                  density="comfortable"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="6">
                <v-select
                  v-model="deployForm.orchestrator"
                  :items="orchestratorOptions"
                  item-title="title"
                  item-value="value"
                  label="Orquestador"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="deployForm.stackName"
                  label="Stack/servicio"
                  density="comfortable"
                  :rules="[rules.required, rules.max80]"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="deployForm.endpoint"
                  label="Endpoint público"
                  density="comfortable"
                  :rules="[rules.required, rules.max180]"
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="deployForm.healthUrl"
                  label="Health URL"
                  density="comfortable"
                  :rules="[rules.required, rules.max220]"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="deployForm.replicas"
                  type="number"
                  label="Replicas"
                  density="comfortable"
                  min="1"
                  max="20"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="deployForm.timeoutSec"
                  type="number"
                  label="Timeout (seg)"
                  density="comfortable"
                  min="10"
                  max="1800"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="deployForm.maxConcurrency"
                  type="number"
                  label="Max concurrencia"
                  density="comfortable"
                  min="1"
                  max="2000"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="deployForm.cpuRequest"
                  label="CPU request"
                  density="comfortable"
                  placeholder="500m"
                  :rules="[rules.max30]"
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="deployForm.memoryRequest"
                  label="Mem request"
                  density="comfortable"
                  placeholder="1Gi"
                  :rules="[rules.max30]"
                />
              </v-col>
            </v-row>

            <div class="toggle-grid mb-2">
              <v-checkbox v-model="deployForm.exposeExternally" hide-details density="compact" label="Exponer externamente" />
              <v-checkbox v-model="deployForm.rollbackOnFail" hide-details density="compact" label="Rollback automático" />
              <v-checkbox v-model="deployForm.enableAutoscaling" hide-details density="compact" label="Autoscaling" />
            </div>

            <v-textarea
              v-model="deployForm.envVarsJson"
              label="Variables de entorno (JSON)"
              auto-grow
              rows="3"
              density="comfortable"
              :error-messages="envVarsError ? [envVarsError] : []"
              placeholder='{"MODEL_CACHE":"/models","LOG_LEVEL":"info"}'
            />

            <v-textarea
              v-model="deployForm.extraJson"
              label="Overrides opcionales (JSON)"
              auto-grow
              rows="3"
              density="comfortable"
              :error-messages="extraJsonError ? [extraJsonError] : []"
              placeholder='{"network":"aibase-net","restartPolicy":"always"}'
            />

            <v-alert
              v-if="numericError"
              class="mb-2"
              type="warning"
              variant="tonal"
              density="comfortable"
              :text="numericError"
            />

            <div class="d-flex align-center ga-2 mt-3 flex-wrap">
              <v-btn
                class="sb-btn primary"
                prepend-icon="mdi-rocket-launch-outline"
                :loading="running"
                :disabled="!canRun"
                @click="triggerDeploy"
              >
                Desplegar servicio
              </v-btn>
              <v-btn class="sb-btn ghost" variant="text" prepend-icon="mdi-content-copy" @click="copyPayloadPreview">
                Copiar inputJson
              </v-btn>
            </div>

            <v-alert
              v-if="runError"
              class="mt-3"
              type="error"
              variant="tonal"
              density="comfortable"
              :text="runError"
            />
            <v-alert
              v-if="runMessage"
              class="mt-3"
              type="success"
              variant="tonal"
              density="comfortable"
              :text="runMessage"
            />
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title class="d-flex align-center justify-space-between">
            Runs de deploy
            <v-btn icon variant="text" :disabled="loadingRuns" @click="loadRuns">
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loadingRuns" class="sb-skeleton" style="height: 120px;"></div>
            <div v-else-if="!deployRuns.length" class="text-caption text-medium-emphasis">No hay corridas de deploy.</div>
            <div v-else class="run-list">
              <button
                v-for="run in deployRuns"
                :key="run.Id || run.id"
                class="run-item"
                :class="{ active: String(selectedDeployRunId) === String(run.Id || run.id) }"
                type="button"
                @click="selectedDeployRunId = run.Id || run.id"
              >
                <div class="run-head">
                  <strong>#{{ run.Id || run.id }}</strong>
                  <v-chip size="small" variant="tonal" :color="statusColor(run.Status || run.status)">
                    {{ run.Status || run.status || 'unknown' }}
                  </v-chip>
                </div>
                <div class="run-meta">
                  {{ formatDate(run.CreatedAt || run.createdAt) }} · progreso {{ run.ProgressPct ?? run.progressPct ?? 0 }}%
                </div>
                <div v-if="run.LastError || run.lastError" class="run-error">
                  {{ run.LastError || run.lastError }}
                </div>
              </button>
            </div>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" lg="5" xl="4" class="deploy-secondary-grid deploy-side-col">
        <v-card class="card">
          <v-card-title class="d-flex align-center justify-space-between">
            Gate de deploy
            <v-btn icon variant="text" :disabled="inferMetricsLoading" @click="loadInferMetrics">
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <ul class="hint-list">
              <li :class="{ ok: !!selectedProjectId }">Proyecto seleccionado</li>
              <li :class="{ ok: evalCompleted }">Evaluación completada</li>
              <li :class="{ ok: canRunStage }">Etapa habilitada en workflow</li>
              <li :class="{ ok: qualityGateOk }">Quality gate de inferencia</li>
              <li :class="{ ok: !envVarsError }">JSON env vars válido</li>
              <li :class="{ ok: !extraJsonError }">JSON de overrides válido</li>
              <li :class="{ ok: !numericError }">Parámetros numéricos válidos</li>
            </ul>
            <div v-if="inferMetricsSummaryText" class="text-caption text-medium-emphasis mt-2">
              {{ inferMetricsSummaryText }}
            </div>
            <v-alert
              v-if="inferMetricsError"
              class="mt-2"
              type="warning"
              variant="tonal"
              density="comfortable"
              :text="inferMetricsError"
            />
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title class="d-flex align-center justify-space-between">
            Estado de workflow
            <v-chip v-if="projectStatus" size="x-small" variant="tonal">{{ projectStatus }}</v-chip>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loadingData" class="sb-skeleton" style="height: 100px;"></div>
            <div v-else-if="!workflowSteps.length" class="text-caption text-medium-emphasis">Sin workflow disponible.</div>
            <div v-else class="workflow-list">
              <div v-for="step in workflowSteps" :key="step.RunType || step.runType" class="workflow-item">
                <span>{{ step.Order || step.order }} · {{ step.Label || step.label }}</span>
                <v-chip size="x-small" variant="tonal" :color="statusColor(step.Status || step.status)">
                  {{ statusLabel(step.Status || step.status) }}
                </v-chip>
              </div>
            </div>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4 deploy-docker-card">
          <v-card-title class="d-flex align-center justify-space-between">
            Control Docker
            <v-btn icon variant="text" :disabled="dockerLoading || dockerBusy" @click="loadDockerStatus">
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text class="docker-panel">
            <div class="docker-overview">
              <v-chip size="small" variant="tonal" :color="dockerRunningServices.length ? 'success' : 'grey'">
                Running: {{ dockerRunningServices.length }}
              </v-chip>
              <v-chip size="small" variant="tonal" :color="dockerServiceItems.length ? 'primary' : 'grey'">
                Services: {{ dockerServiceItems.length }}
              </v-chip>
            </div>
            <v-alert
              v-if="dockerError"
              class="mt-1"
              type="warning"
              variant="tonal"
              density="comfortable"
              :text="dockerError"
            />
            <v-alert
              v-if="dockerMessage"
              class="mt-1"
              type="success"
              variant="tonal"
              density="comfortable"
              :text="dockerMessage"
            />

            <v-row dense>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="dockerStackName"
                  label="Stack docker"
                  density="compact"
                  :disabled="dockerBusy"
                />
              </v-col>
              <v-col cols="12" md="6">
                <div class="docker-stack-actions">
                  <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-play" :loading="dockerActionLoading === 'up'" :disabled="dockerBusy" @click="runDockerStackAction('up')">
                    Up
                  </v-btn>
                  <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-stop" :loading="dockerActionLoading === 'down'" :disabled="dockerBusy" @click="runDockerStackAction('down')">
                    Down
                  </v-btn>
                  <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-restart" :loading="dockerActionLoading === 'restart'" :disabled="dockerBusy" @click="runDockerStackAction('restart')">
                    Restart
                  </v-btn>
                </div>
              </v-col>
            </v-row>

            <v-row dense class="mt-1">
              <v-col cols="12" md="6">
                <v-select
                  v-model="dockerSelectedService"
                  :items="dockerServiceItems"
                  label="Servicio"
                  density="compact"
                  clearable
                  :disabled="dockerBusy"
                />
              </v-col>
              <v-col cols="12" md="6">
                <div class="docker-service-actions">
                  <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-play-circle-outline" :loading="dockerActionLoading === 'service_start'" :disabled="dockerBusy || !dockerSelectedService" @click="runDockerServiceAction('start')">
                    Start
                  </v-btn>
                  <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-stop-circle-outline" :loading="dockerActionLoading === 'service_stop'" :disabled="dockerBusy || !dockerSelectedService" @click="runDockerServiceAction('stop')">
                    Stop
                  </v-btn>
                  <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-restart" :loading="dockerActionLoading === 'service_restart'" :disabled="dockerBusy || !dockerSelectedService" @click="runDockerServiceAction('restart')">
                    Restart
                  </v-btn>
                </div>
              </v-col>
            </v-row>

            <v-row dense class="mt-1">
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="dockerLogsTail"
                  type="number"
                  label="Tail logs"
                  density="compact"
                  min="20"
                  max="2000"
                  :disabled="dockerBusy"
                />
              </v-col>
              <v-col cols="12" md="8">
                <div class="docker-logs-actions">
                  <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-text-box-search-outline" :loading="dockerActionLoading === 'logs'" :disabled="dockerBusy" @click="loadDockerLogs">
                    Ver logs
                  </v-btn>
                  <v-btn class="sb-btn ghost" variant="text" size="small" prepend-icon="mdi-content-copy" :disabled="!dockerLogs" @click="copyText(dockerLogs)">
                    Copiar logs
                  </v-btn>
                </div>
              </v-col>
            </v-row>

            <v-expansion-panels class="docker-advanced-panel mt-1">
              <v-expansion-panel>
                <v-expansion-panel-title>
                  Contexto compose/env
                </v-expansion-panel-title>
                <v-expansion-panel-text>
                  <v-row dense>
                    <v-col cols="12">
                      <v-text-field
                        v-model="dockerComposePath"
                        label="Compose file (bundle)"
                        density="compact"
                        :disabled="dockerBusy"
                        placeholder="/abs/path/docker-compose.yml"
                      />
                    </v-col>
                    <v-col cols="12">
                      <v-text-field
                        v-model="dockerEnvFilePath"
                        label="Env file (opcional)"
                        density="compact"
                        :disabled="dockerBusy"
                        placeholder="/abs/path/.env"
                      />
                    </v-col>
                  </v-row>

                  <div v-if="dockerComposeFileResolved || dockerEnvFileResolved" class="text-caption text-medium-emphasis mt-1">
                    <div v-if="dockerComposeFileResolved">Compose: {{ dockerComposeFileResolved }}</div>
                    <div v-if="dockerEnvFileResolved">Env: {{ dockerEnvFileResolved }}</div>
                  </div>
                </v-expansion-panel-text>
              </v-expansion-panel>

              <v-expansion-panel>
                <v-expansion-panel-title>
                  Estado contenedores ({{ dockerPsRows.length }})
                </v-expansion-panel-title>
                <v-expansion-panel-text>
                  <div v-if="dockerPsRows.length" class="docker-ps-list">
                    <div v-for="row in dockerPsRows" :key="`${row.Name}-${row.Service}`" class="docker-ps-item">
                      <div class="docker-ps-title">{{ row.Service || row.Name || 'service' }}</div>
                      <v-chip size="x-small" variant="tonal" :color="dockerStateColor(row.State)">
                        {{ row.State || 'unknown' }}
                      </v-chip>
                      <div class="docker-ps-meta">{{ row.Status || 'sin status' }}</div>
                    </div>
                  </div>
                  <div v-else class="text-caption text-medium-emphasis">
                    Sin contenedores para el stack actual.
                  </div>
                </v-expansion-panel-text>
              </v-expansion-panel>

              <v-expansion-panel>
                <v-expansion-panel-title>
                  Salida de logs
                </v-expansion-panel-title>
                <v-expansion-panel-text>
                  <div v-if="dockerLogs" class="preview-box docker-log-box">
                    <pre>{{ dockerLogs }}</pre>
                  </div>
                  <div v-else class="text-caption text-medium-emphasis">
                    No hay logs cargados todavía.
                  </div>
                </v-expansion-panel-text>
              </v-expansion-panel>
            </v-expansion-panels>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-2">
            <span>Resultado de despliegue</span>
            <div class="d-flex align-center ga-2 flex-wrap">
              <v-chip
                v-if="deployResultSummary.status"
                size="small"
                variant="tonal"
                :color="deployResultSummary.statusColor"
              >
                {{ deployResultSummary.status }}
              </v-chip>
              <v-chip v-if="deployResultSummary.service" size="small" variant="tonal" color="primary">
                Servicio: {{ deployResultSummary.service }}
              </v-chip>
            </div>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="!selectedDeployRun" class="text-caption text-medium-emphasis">
              Selecciona un run de deploy para ver su resultado.
            </div>
            <template v-else>
              <div class="result-meta">
                <div><strong>Run:</strong> #{{ selectedDeployRun.Id || selectedDeployRun.id }}</div>
                <div><strong>Fecha:</strong> {{ formatDate(selectedDeployRun.CreatedAt || selectedDeployRun.createdAt) }}</div>
              </div>
              <div class="result-meta mt-2" v-if="deployResultSummary.endpoint || deployResultSummary.health">
                <div v-if="deployResultSummary.endpoint">
                  <strong>Endpoint:</strong>
                  <a :href="deployResultSummary.endpoint" target="_blank" rel="noopener noreferrer">{{ deployResultSummary.endpoint }}</a>
                </div>
                <div v-if="deployResultSummary.health">
                  <strong>Health:</strong>
                  <a :href="deployResultSummary.health" target="_blank" rel="noopener noreferrer">{{ deployResultSummary.health }}</a>
                </div>
              </div>
              <div class="d-flex align-center ga-2 mt-3 flex-wrap">
                <v-btn
                  class="sb-btn"
                  variant="tonal"
                  prepend-icon="mdi-open-in-new"
                  :disabled="!deployResultSummary.endpoint"
                  @click="openEndpoint"
                >
                  Abrir endpoint
                </v-btn>
                <v-btn
                  class="sb-btn"
                  variant="tonal"
                  prepend-icon="mdi-heart-pulse"
                  :disabled="!deployResultSummary.health"
                  @click="openHealth"
                >
                  Ver health
                </v-btn>
              </div>
              <div v-if="deployResultSummary.command" class="mt-3">
                <div class="d-flex align-center justify-space-between mb-1">
                  <strong>Comando</strong>
                  <v-btn class="sb-btn ghost" variant="text" size="small" prepend-icon="mdi-content-copy" @click="copyText(deployResultSummary.command)">
                    Copiar
                  </v-btn>
                </div>
                <div class="preview-box command-box">
                  <pre>{{ deployResultSummary.command }}</pre>
                </div>
              </div>
              <div v-if="deployResultSummary.logs" class="mt-3">
                <div class="d-flex align-center justify-space-between mb-1">
                  <strong>Logs</strong>
                  <v-btn class="sb-btn ghost" variant="text" size="small" prepend-icon="mdi-content-copy" @click="copyText(deployResultSummary.logs)">
                    Copiar
                  </v-btn>
                </div>
                <div class="preview-box logs-box">
                  <pre>{{ deployResultSummary.logs }}</pre>
                </div>
              </div>
              <v-alert
                v-if="deployResultSummary.errors"
                class="mt-3"
                type="warning"
                variant="tonal"
                density="comfortable"
                :text="deployResultSummary.errors"
              />
              <div class="preview-box mt-3">
                <pre>{{ selectedDeployOutputPretty }}</pre>
              </div>
            </template>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-2">
            <span>Activos de consumo</span>
            <div class="d-flex align-center ga-2 flex-wrap">
              <v-chip v-if="deployAssetsSummary.stackName" size="x-small" variant="tonal" color="primary">
                {{ deployAssetsSummary.stackName }}
              </v-chip>
              <v-btn
                class="sb-btn ghost"
                variant="text"
                size="small"
                prepend-icon="mdi-refresh"
                :loading="deployAssetsLoading"
                :disabled="!selectedProjectId || exportingBundle"
                @click="loadDeployAssets"
              >
                Actualizar
              </v-btn>
              <v-btn
                class="sb-btn"
                variant="tonal"
                size="small"
                prepend-icon="mdi-archive-arrow-down-outline"
                :loading="exportingBundle"
                :disabled="!selectedProjectId || deployAssetsLoading"
                @click="exportDockerBundle"
              >
                Generar bundle Docker
              </v-btn>
            </div>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="deployAssetsLoading" class="sb-skeleton" style="height: 120px;"></div>
            <v-alert
              v-else-if="deployAssetsError"
              type="warning"
              variant="tonal"
              density="comfortable"
              :text="deployAssetsError"
            />
            <v-alert
              v-else-if="exportBundleError"
              type="warning"
              variant="tonal"
              density="comfortable"
              :text="exportBundleError"
            />
            <div v-else>
              <v-alert
                v-if="exportBundleMessage"
                class="mb-2"
                type="success"
                variant="tonal"
                density="comfortable"
                :text="exportBundleMessage"
              />
              <v-alert
                v-if="deployAssetsSummary.engineNotice"
                class="mb-2"
                type="info"
                variant="tonal"
                density="comfortable"
                :text="deployAssetsSummary.engineNotice"
              />
              <div class="result-meta">
                <div v-if="deployAssetsSummary.endpoint">
                  <strong>Endpoint:</strong>
                  <a :href="deployAssetsSummary.endpoint" target="_blank" rel="noopener noreferrer">{{ deployAssetsSummary.endpoint }}</a>
                </div>
                <div v-if="deployAssetsSummary.health">
                  <strong>Health:</strong>
                  <a :href="deployAssetsSummary.health" target="_blank" rel="noopener noreferrer">{{ deployAssetsSummary.health }}</a>
                </div>
                <div v-if="deployAssetsSummary.bundleDir"><strong>Bundle:</strong> {{ deployAssetsSummary.bundleDir }}</div>
                <div v-if="deployAssetsSummary.composeFile"><strong>Compose:</strong> {{ deployAssetsSummary.composeFile }}</div>
                <div v-if="deployAssetsSummary.envFile"><strong>Env:</strong> {{ deployAssetsSummary.envFile }}</div>
                <div v-if="deployAssetsSummary.engineEndpoint"><strong>Engine:</strong> {{ deployAssetsSummary.engineEndpoint }}</div>
                <div v-if="deployAssetsSummary.imageTag"><strong>Image:</strong> {{ deployAssetsSummary.imageTag }}</div>
                <div v-if="deployAssetsSummary.createdAt"><strong>Generado:</strong> {{ formatDate(deployAssetsSummary.createdAt) }}</div>
              </div>

              <div v-if="deployAssetsSummary.dockerCommand" class="mt-3">
                <div class="d-flex align-center justify-space-between mb-1">
                  <strong>Docker command</strong>
                  <v-btn class="sb-btn ghost" variant="text" size="small" prepend-icon="mdi-content-copy" @click="copyText(deployAssetsSummary.dockerCommand)">
                    Copiar
                  </v-btn>
                </div>
                <div class="preview-box command-box">
                  <pre>{{ deployAssetsSummary.dockerCommand }}</pre>
                </div>
              </div>

              <v-tabs v-model="deploySnippetTab" class="mt-3" density="comfortable">
                <v-tab value="curl">cURL</v-tab>
                <v-tab value="js">JavaScript</v-tab>
                <v-tab value="py">Python</v-tab>
              </v-tabs>
              <v-window v-model="deploySnippetTab" class="mt-2">
                <v-window-item value="curl">
                  <div class="d-flex align-center justify-space-between mb-1">
                    <strong>Snippet cURL</strong>
                    <v-btn class="sb-btn ghost" variant="text" size="small" prepend-icon="mdi-content-copy" @click="copyText(deployAssetsSummary.curlSnippet)">
                      Copiar
                    </v-btn>
                  </div>
                  <div class="preview-box">
                    <pre>{{ deployAssetsSummary.curlSnippet }}</pre>
                  </div>
                </v-window-item>
                <v-window-item value="js">
                  <div class="d-flex align-center justify-space-between mb-1">
                    <strong>Snippet JS</strong>
                    <v-btn class="sb-btn ghost" variant="text" size="small" prepend-icon="mdi-content-copy" @click="copyText(deployAssetsSummary.javaScriptSnippet)">
                      Copiar
                    </v-btn>
                  </div>
                  <div class="preview-box">
                    <pre>{{ deployAssetsSummary.javaScriptSnippet }}</pre>
                  </div>
                </v-window-item>
                <v-window-item value="py">
                  <div class="d-flex align-center justify-space-between mb-1">
                    <strong>Snippet Python</strong>
                    <v-btn class="sb-btn ghost" variant="text" size="small" prepend-icon="mdi-content-copy" @click="copyText(deployAssetsSummary.pythonSnippet)">
                      Copiar
                    </v-btn>
                  </div>
                  <div class="preview-box">
                    <pre>{{ deployAssetsSummary.pythonSnippet }}</pre>
                  </div>
                </v-window-item>
              </v-window>
            </div>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title class="d-flex align-center justify-space-between">
            Preview inputJson
            <v-btn class="sb-btn ghost" variant="text" prepend-icon="mdi-content-copy" @click="copyPayloadPreview">Copiar</v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div class="preview-box">
              <pre>{{ payloadPreview }}</pre>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import runtimeApi from '../../api/runtime.service'
import OptionGuide from '../../components/Workflow/OptionGuide.vue'

const router = useRouter()
const route = useRoute()

const loadingData = ref(false)
const loadingRuns = ref(false)
const running = ref(false)

const error = ref('')
const runMessage = ref('')
const runError = ref('')
const dockerError = ref('')
const dockerMessage = ref('')

const projects = ref([])
const selectedProjectId = ref(null)
const projectWorkflow = ref(null)
const runs = ref([])
const selectedDeployRunId = ref(null)
const dockerStatus = ref(null)
const dockerStackName = ref('')
const dockerComposePath = ref('')
const dockerEnvFilePath = ref('')
const dockerSelectedService = ref('')
const dockerLogs = ref('')
const dockerLogsTail = ref(200)
const dockerActionLoading = ref('')
const dockerLoading = ref(false)
const deployAssetsLoading = ref(false)
const deployAssetsError = ref('')
const deployAssetsData = ref(null)
const inferMetricsLoading = ref(false)
const inferMetricsError = ref('')
const inferMetrics = ref(null)
const exportingBundle = ref(false)
const exportBundleError = ref('')
const exportBundleMessage = ref('')
const deploySnippetTab = ref('curl')

const deployForm = reactive({
  targetEnv: 'dev',
  orchestrator: 'docker_compose',
  stackName: 'aibase-stack',
  endpoint: 'http://localhost:8010',
  healthUrl: 'http://localhost:8010/health',
  replicas: 1,
  timeoutSec: 180,
  maxConcurrency: 100,
  cpuRequest: '500m',
  memoryRequest: '1Gi',
  exposeExternally: true,
  rollbackOnFail: true,
  enableAutoscaling: false,
  envVarsJson: '{}',
  extraJson: '{}'
})

const targetEnvOptions = [
  { title: 'dev', value: 'dev' },
  { title: 'staging', value: 'staging' },
  { title: 'prod', value: 'prod' }
]

const orchestratorOptions = [
  { title: 'docker-compose', value: 'docker_compose' },
  { title: 'kubernetes', value: 'k8s' },
  { title: 'nomad', value: 'nomad' }
]

const rules = {
  required: v => !!String(v ?? '').trim() || 'Campo requerido',
  max30: v => String(v ?? '').length <= 30 || 'Máximo 30 caracteres',
  max80: v => String(v ?? '').length <= 80 || 'Máximo 80 caracteres',
  max180: v => String(v ?? '').length <= 180 || 'Máximo 180 caracteres',
  max220: v => String(v ?? '').length <= 220 || 'Máximo 220 caracteres'
}

const optionGuideItems = [
  { label: 'Ambiente y orquestador', description: 'Indica dónde desplegar (dev/staging/prod) y con qué plataforma.' },
  { label: 'Stack/servicio', description: 'Nombre operativo del servicio para identificarlo en ejecución.' },
  { label: 'Endpoint y Health URL', description: 'Rutas públicas para consumo y verificación de disponibilidad.' },
  { label: 'Replicas / Timeout / Concurrencia', description: 'Capacidad y límites operativos del servicio desplegado.' },
  { label: 'CPU/Mem request', description: 'Recursos mínimos reservados para evitar inestabilidad.' },
  { label: 'Quality gate', description: 'Antes de deploy valida métricas recientes de inferencia desde Playground.' },
  { label: 'Env vars / Overrides JSON', description: 'Configuración avanzada del runtime sin tocar código.' }
]

function normalizeRecord(record) {
  if (!record || typeof record !== 'object') return record
  const copy = { ...record }
  const keyMap = new Map(Object.keys(copy).map(k => [k.toLowerCase(), k]))
  const ensure = name => {
    if (copy[name] !== undefined) return
    const match = keyMap.get(String(name).toLowerCase())
    if (match) copy[name] = copy[match]
  }
  ;['Id', 'Name', 'RunType', 'Status', 'ProgressPct', 'CreatedAt', 'UpdatedAt', 'Inputjson', 'Outputjson', 'Lasterror'].forEach(ensure)
  return copy
}

function errorText(err, fallback = 'Error de comunicación.') {
  const msg = err?.response?.data?.error
    || err?.response?.data?.message
    || (typeof err?.response?.data === 'string' ? err.response.data : null)
    || err?.message
  return String(msg || fallback)
}

function dockerErrorText(err, fallback) {
  const base = errorText(err, fallback)
  const stderr = String(err?.response?.data?.stderr || '').trim()
  const command = String(err?.response?.data?.command || '').trim()
  if (!stderr && !command) return base

  const chunks = [base]
  if (stderr) chunks.push(`Detalle: ${stderr}`)
  if (command) chunks.push(`Comando: ${command}`)
  return chunks.join('\n')
}

function statusColor(status) {
  const value = String(status || '').toLowerCase()
  if (value === 'completed' || value === 'deployed' || value === 'ready') return 'success'
  if (value === 'running') return 'warning'
  if (value === 'error' || value === 'failed') return 'error'
  if (value === 'blocked') return 'grey'
  if (value === 'na') return 'blue-grey'
  return 'primary'
}

function statusLabel(status) {
  const value = String(status || '').toLowerCase()
  if (value === 'completed' || value === 'deployed' || value === 'ready') return 'Completado'
  if (value === 'running') return 'En curso'
  if (value === 'error' || value === 'failed') return 'Error'
  if (value === 'blocked') return 'Bloqueado'
  if (value === 'na') return 'No aplica'
  return 'Pendiente'
}

function dockerStateColor(state) {
  const value = String(state || '').toLowerCase()
  if (value === 'running') return 'success'
  if (value === 'exited' || value === 'dead') return 'error'
  if (value === 'created' || value === 'restarting') return 'warning'
  return 'grey'
}

function formatDate(value) {
  if (!value) return 'Sin fecha'
  return new Date(value).toLocaleString('es-AR')
}

function parseJson(text, fallback = null) {
  try {
    const raw = String(text || '').trim()
    if (!raw) return { ok: true, value: fallback }
    return { ok: true, value: JSON.parse(raw) }
  } catch (err) {
    return { ok: false, value: fallback, message: err?.message || 'JSON inválido' }
  }
}

function pickFirst(...values) {
  for (const value of values) {
    if (value === null || value === undefined) continue
    const text = String(value).trim()
    if (text) return text
  }
  return ''
}

function toTextBlock(value) {
  if (!value) return ''
  if (Array.isArray(value)) return value.map(item => String(item ?? '')).join('\n')
  if (typeof value === 'object') {
    try {
      return JSON.stringify(value, null, 2)
    } catch {
      return String(value)
    }
  }
  return String(value)
}

function resolveDockerStackName() {
  const value = String(dockerStackName.value || deployForm.stackName || 'aibase-stack').trim()
  return value || 'aibase-stack'
}

function normalizeOptionalPath(value) {
  const text = String(value || '').trim()
  return text || null
}

function buildDockerContextPayload() {
  return {
    stackName: resolveDockerStackName(),
    composeFile: normalizeOptionalPath(dockerComposePath.value),
    envFile: normalizeOptionalPath(dockerEnvFilePath.value)
  }
}

function applyDockerContextFromAssets(force = false) {
  const stackName = String(
    deployAssetsData.value?.StackName
    || deployAssetsData.value?.stackName
    || deployAssetsData.value?.ServiceName
    || deployAssetsData.value?.serviceName
    || ''
  ).trim()
  const composePath = String(
    deployAssetsData.value?.ComposeFile
    || deployAssetsData.value?.composeFile
    || ''
  ).trim()
  const envPath = String(
    deployAssetsData.value?.EnvFile
    || deployAssetsData.value?.envFile
    || ''
  ).trim()

  if (force || !String(dockerStackName.value || '').trim()) {
    if (stackName) dockerStackName.value = stackName
  }
  if (force || !String(dockerComposePath.value || '').trim()) {
    if (composePath) dockerComposePath.value = composePath
  }
  if (force || !String(dockerEnvFilePath.value || '').trim()) {
    if (envPath) dockerEnvFilePath.value = envPath
  }
}

const projectItems = computed(() => projects.value.map(project => ({
  title: project.Name || project.name || `Proyecto #${project.Id || project.id}`,
  value: project.Id || project.id
})))

const workflowSteps = computed(() => {
  const raw = projectWorkflow.value?.Steps || projectWorkflow.value?.steps
  return Array.isArray(raw) ? raw : []
})

const projectStatus = computed(() => String(projectWorkflow.value?.ProjectStatus || projectWorkflow.value?.projectStatus || 'draft'))

const evalStep = computed(() => workflowSteps.value.find(step =>
  String(step.RunType || step.runType || '').toLowerCase() === 'eval_run'
))

const deployStep = computed(() => workflowSteps.value.find(step =>
  String(step.RunType || step.runType || '').toLowerCase() === 'deploy_service'
))

const evalCompleted = computed(() => {
  const status = String(evalStep.value?.Status || evalStep.value?.status || '').toLowerCase()
  return status === 'completed'
})

const canRunStage = computed(() => {
  if (!deployStep.value) return false
  const enabled = Boolean(deployStep.value.Enabled ?? deployStep.value.enabled)
  const available = Boolean(deployStep.value.Available ?? deployStep.value.available)
  const status = String(deployStep.value.Status || deployStep.value.status || '').toLowerCase()
  return enabled && (available || status === 'completed')
})

const envVarsParsed = computed(() => parseJson(deployForm.envVarsJson, {}))
const envVarsError = computed(() => (envVarsParsed.value.ok ? '' : 'Env vars JSON inválido'))

const extraParsed = computed(() => parseJson(deployForm.extraJson, {}))
const extraJsonError = computed(() => (extraParsed.value.ok ? '' : 'Overrides JSON inválido'))

const numericError = computed(() => {
  if (Number(deployForm.replicas || 0) < 1) return 'Replicas debe ser >= 1.'
  if (Number(deployForm.timeoutSec || 0) < 10) return 'Timeout debe ser >= 10.'
  if (Number(deployForm.maxConcurrency || 0) < 1) return 'Max concurrencia debe ser >= 1.'
  return ''
})

const canRun = computed(() =>
  Boolean(selectedProjectId.value) &&
  Boolean(String(deployForm.stackName || '').trim()) &&
  Boolean(String(deployForm.endpoint || '').trim()) &&
  Boolean(String(deployForm.healthUrl || '').trim()) &&
  !envVarsError.value &&
  !extraJsonError.value &&
  !numericError.value &&
  canRunStage.value &&
  evalCompleted.value &&
  qualityGateOk.value
)

const deployRuns = computed(() => runs.value.filter(run =>
  String(run.RunType || run.runType || '').toLowerCase() === 'deploy_service'
))

const dockerBusy = computed(() => dockerLoading.value || !!dockerActionLoading.value)

const dockerPsRows = computed(() => {
  const raw = dockerStatus.value?.ps
  return Array.isArray(raw) ? raw : []
})

const dockerComposeFileResolved = computed(() =>
  pickFirst(dockerStatus.value?.composeFile, dockerComposePath.value)
)

const dockerEnvFileResolved = computed(() =>
  pickFirst(dockerStatus.value?.envFile, dockerEnvFilePath.value)
)

const dockerServiceItems = computed(() => {
  const set = new Set()
  const configured = dockerStatus.value?.services
  if (Array.isArray(configured)) {
    configured.forEach(service => {
      const value = String(service || '').trim()
      if (value) set.add(value)
    })
  }
  dockerPsRows.value.forEach(row => {
    const value = String(row?.Service || row?.service || row?.Name || row?.name || '').trim()
    if (value) set.add(value)
  })
  return Array.from(set)
})

const dockerRunningServices = computed(() =>
  dockerPsRows.value.filter(row => String(row?.State || '').toLowerCase() === 'running')
)

const selectedDeployRun = computed(() => {
  if (!deployRuns.value.length) return null
  if (!selectedDeployRunId.value) return deployRuns.value[0]
  return deployRuns.value.find(run => String(run.Id || run.id) === String(selectedDeployRunId.value)) || deployRuns.value[0]
})

const selectedDeployOutputParsed = computed(() => {
  const output = selectedDeployRun.value?.Outputjson || selectedDeployRun.value?.outputjson || ''
  return parseJson(output, null)
})

const selectedDeployInputParsed = computed(() => {
  const input = selectedDeployRun.value?.Inputjson || selectedDeployRun.value?.inputjson || ''
  return parseJson(input, null)
})

const selectedDeployOutputPretty = computed(() => {
  if (!selectedDeployRun.value) return ''
  const output = selectedDeployRun.value.Outputjson || selectedDeployRun.value.outputjson || selectedDeployRun.value.LastError || selectedDeployRun.value.lastError || ''
  const parsed = parseJson(output, null)
  if (parsed.ok && parsed.value && typeof parsed.value === 'object') return JSON.stringify(parsed.value, null, 2)
  return String(output || 'Sin output')
})

const deployResultSummary = computed(() => {
  const obj = selectedDeployOutputParsed.value.ok ? selectedDeployOutputParsed.value.value : null
  const input = selectedDeployInputParsed.value.ok ? selectedDeployInputParsed.value.value : null
  const deploy = obj?.deploy || {}
  const status = String(
    obj?.status
    || deploy?.status
    || selectedDeployRun.value?.Status
    || selectedDeployRun.value?.status
    || ''
  ).toLowerCase()
  const endpoint = pickFirst(deploy?.endpoint, obj?.endpoint, input?.endpoint)
  const health = pickFirst(deploy?.health, obj?.health, input?.healthUrl)
  const service = pickFirst(deploy?.service, obj?.service, deploy?.stackName, obj?.stackName, input?.stackName)
  const command = pickFirst(deploy?.command, obj?.command, obj?.deployCommand, deploy?.deployCommand)
  const logs = pickFirst(
    toTextBlock(deploy?.logs),
    toTextBlock(obj?.logs),
    toTextBlock(obj?.stdout),
    toTextBlock(obj?.stderr)
  )
  const errors = pickFirst(
    toTextBlock(deploy?.errors),
    toTextBlock(obj?.errors),
    toTextBlock(selectedDeployRun.value?.LastError || selectedDeployRun.value?.lastError)
  )
  return {
    status: status || '',
    statusColor: statusColor(status || 'pending'),
    service,
    endpoint,
    health,
    command,
    logs,
    errors
  }
})

const payloadPreview = computed(() => JSON.stringify(buildRunInputPayload(), null, 2))

const deployAssetsSummary = computed(() => {
  const raw = deployAssetsData.value || {}
  return {
    stackName: pickFirst(raw.StackName, raw.stackName, deployForm.stackName),
    serviceName: pickFirst(raw.ServiceName, raw.serviceName, raw.StackName, raw.stackName),
    imageTag: pickFirst(raw.ImageTag, raw.imageTag),
    bundleDir: pickFirst(raw.BundleDir, raw.bundleDir),
    composeFile: pickFirst(raw.ComposeFile, raw.composeFile),
    envFile: pickFirst(raw.EnvFile, raw.envFile),
    engineEndpoint: pickFirst(raw.EngineEndpoint, raw.engineEndpoint),
    engineNotice: pickFirst(raw.EngineNotice, raw.engineNotice),
    endpoint: pickFirst(raw.Endpoint, raw.endpoint, deployResultSummary.value.endpoint),
    health: pickFirst(raw.Health, raw.health, deployResultSummary.value.health),
    dockerCommand: pickFirst(raw.DockerCommand, raw.dockerCommand, deployResultSummary.value.command),
    curlSnippet: pickFirst(raw.CurlSnippet, raw.curlSnippet),
    javaScriptSnippet: pickFirst(raw.JavaScriptSnippet, raw.javaScriptSnippet, raw.javascriptSnippet),
    pythonSnippet: pickFirst(raw.PythonSnippet, raw.pythonSnippet),
    createdAt: pickFirst(raw.CreatedAt, raw.createdAt)
  }
})

const inferMetricsGate = computed(() => {
  const raw = inferMetrics.value?.gate
  if (!raw || typeof raw !== 'object') return null
  return {
    ok: Boolean(raw.ok),
    reason: pickFirst(raw.reason, raw.message)
  }
})

const inferMetricsSummary = computed(() => {
  const raw = inferMetrics.value?.summary
  if (!raw || typeof raw !== 'object') return null
  return {
    total: Number(raw.total || 0),
    successRate: Number(raw.successRate || 0),
    fallbackRate: Number(raw.fallbackRate || 0),
    avgLatencyMs: Number(raw.avgLatencyMs || 0)
  }
})

const qualityGateOk = computed(() => {
  if (!selectedProjectId.value) return false
  if (inferMetricsLoading.value) return false
  if (inferMetricsError.value) return false
  if (inferMetricsGate.value) return inferMetricsGate.value.ok
  return false
})

const inferMetricsSummaryText = computed(() => {
  if (!inferMetricsSummary.value) return ''
  const summary = inferMetricsSummary.value
  const gate = inferMetricsGate.value
  const gateLabel = gate ? (gate.ok ? 'ok' : `revisar (${gate.reason || 'gate'})`) : 'sin gate'
  return `Muestras: ${summary.total} · Success: ${(summary.successRate * 100).toFixed(0)}% · Fallback: ${(summary.fallbackRate * 100).toFixed(0)}% · Avg latency: ${summary.avgLatencyMs} ms · Gate: ${gateLabel}`
})

function buildRunInputPayload() {
  return {
    targetEnv: deployForm.targetEnv,
    orchestrator: deployForm.orchestrator,
    stackName: String(deployForm.stackName || '').trim(),
    endpoint: String(deployForm.endpoint || '').trim(),
    healthUrl: String(deployForm.healthUrl || '').trim(),
    runtime: {
      replicas: Number(deployForm.replicas || 0),
      timeoutSec: Number(deployForm.timeoutSec || 0),
      maxConcurrency: Number(deployForm.maxConcurrency || 0),
      cpuRequest: String(deployForm.cpuRequest || '').trim() || null,
      memoryRequest: String(deployForm.memoryRequest || '').trim() || null
    },
    options: {
      exposeExternally: Boolean(deployForm.exposeExternally),
      rollbackOnFail: Boolean(deployForm.rollbackOnFail),
      enableAutoscaling: Boolean(deployForm.enableAutoscaling)
    },
    envVars: envVarsParsed.value.ok ? (envVarsParsed.value.value || {}) : {},
    overrides: extraParsed.value.ok ? (extraParsed.value.value || {}) : {}
  }
}

let refreshTimer = null

const selectedDeployStatus = computed(() => String(selectedDeployRun.value?.Status || selectedDeployRun.value?.status || '').toLowerCase())
const shouldPollRuns = computed(() => ['running', 'queued', 'processing'].includes(selectedDeployStatus.value))

function clearRefreshTimer() {
  if (refreshTimer) {
    clearInterval(refreshTimer)
    refreshTimer = null
  }
}

function ensureRefreshTimer() {
  clearRefreshTimer()
  if (!shouldPollRuns.value) return
  refreshTimer = setInterval(async () => {
    if (loadingRuns.value || running.value || dockerLoading.value || dockerActionLoading.value) return
    await Promise.all([loadWorkflow(selectedProjectId.value), loadRuns(), loadDockerStatus(), loadInferMetrics()])
  }, 3000)
}

function suggestDefaultsFromProject() {
  if (!selectedProjectId.value) return
  if (!String(deployForm.stackName || '').trim()) {
    deployForm.stackName = 'aibase-stack'
  }
}

async function loadProjects() {
  const response = await runtimeApi.list('projects')
  projects.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
}

async function loadWorkflow(projectId) {
  if (!projectId) {
    projectWorkflow.value = null
    return
  }
  const response = await runtimeApi.getProjectWorkflow(projectId)
  projectWorkflow.value = response?.data || null
}

async function loadRuns() {
  if (!selectedProjectId.value) {
    runs.value = []
    selectedDeployRunId.value = null
    return
  }
  loadingRuns.value = true
  try {
    const response = await runtimeApi.listProjectRuns(selectedProjectId.value, 50)
    runs.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
    const topDeploy = runs.value.find(run => String(run.RunType || run.runType || '').toLowerCase() === 'deploy_service')
    selectedDeployRunId.value = topDeploy ? (topDeploy.Id || topDeploy.id) : null
  } finally {
    loadingRuns.value = false
  }
}

async function loadDeployAssets() {
  deployAssetsLoading.value = true
  deployAssetsError.value = ''
  try {
    if (!selectedProjectId.value) {
      deployAssetsData.value = null
      return
    }
    const response = await runtimeApi.deployAssets(selectedProjectId.value)
    deployAssetsData.value = response?.data || null
    applyDockerContextFromAssets(false)
  } catch (err) {
    deployAssetsData.value = null
    deployAssetsError.value = errorText(err, 'No se pudieron obtener los activos de deploy.')
  } finally {
    deployAssetsLoading.value = false
  }
}

async function loadInferMetrics() {
  inferMetricsLoading.value = true
  inferMetricsError.value = ''
  try {
    if (!selectedProjectId.value) {
      inferMetrics.value = null
      return
    }
    const response = await runtimeApi.inferMetrics(selectedProjectId.value, { take: 40 })
    inferMetrics.value = response?.data || null
  } catch (err) {
    inferMetrics.value = null
    inferMetricsError.value = errorText(err, 'No se pudieron obtener métricas de inferencia.')
  } finally {
    inferMetricsLoading.value = false
  }
}

function inferPortFromUrl(urlText, fallback = 8010) {
  const raw = String(urlText || '').trim()
  if (!raw) return fallback
  try {
    const normalized = raw.includes('://') ? raw : `http://${raw}`
    const parsed = new URL(normalized)
    if (parsed.port) {
      const asNumber = Number(parsed.port)
      if (Number.isFinite(asNumber) && asNumber >= 1 && asNumber <= 65535) return asNumber
    }
    return parsed.protocol === 'https:' ? 443 : 80
  } catch {
    return fallback
  }
}

function buildDeployExportPayload() {
  const overrides = extraParsed.value.ok && extraParsed.value.value && typeof extraParsed.value.value === 'object'
    ? extraParsed.value.value
    : {}
  const envVars = envVarsParsed.value.ok && envVarsParsed.value.value && typeof envVarsParsed.value.value === 'object'
    ? envVarsParsed.value.value
    : {}

  const serviceName = String(deployForm.stackName || '').trim() || 'aibase-stack'
  const endpoint = String(deployForm.endpoint || '').trim() || `http://localhost:${inferPortFromUrl('', 8010)}`
  const healthUrl = String(deployForm.healthUrl || '').trim() || `${endpoint.replace(/\/+$/, '')}/health`

  const hostPort = inferPortFromUrl(endpoint, 8010)
  const containerPort = Number(overrides.containerPort || 8010)
  const imageTag = String(overrides.imageTag || overrides.dockerImage || '').trim()

  const extraEnv = {}
  Object.entries(envVars).forEach(([key, value]) => {
    const normalizedKey = String(key || '').trim()
    if (!normalizedKey) return
    extraEnv[normalizedKey] = value == null ? '' : String(value)
  })

  return {
    serviceName,
    imageTag: imageTag || null,
    hostPort,
    containerPort: Number.isFinite(containerPort) && containerPort > 0 ? containerPort : 8010,
    endpoint,
    healthUrl,
    extraEnv
  }
}

async function exportDockerBundle() {
  if (!selectedProjectId.value) return false

  exportingBundle.value = true
  exportBundleError.value = ''
  exportBundleMessage.value = ''

  try {
    const payload = buildDeployExportPayload()
    const response = await runtimeApi.exportDeployBundle(selectedProjectId.value, payload)
    deployAssetsData.value = response?.data || null
    deployAssetsError.value = ''

    const nextStack = String(
      deployAssetsData.value?.StackName
      || deployAssetsData.value?.stackName
      || payload.serviceName
      || deployForm.stackName
      || ''
    ).trim()
    if (nextStack) {
      dockerStackName.value = nextStack
    }
    applyDockerContextFromAssets(true)

    const notice = String(
      deployAssetsData.value?.EngineNotice
      || deployAssetsData.value?.engineNotice
      || ''
    ).trim()
    exportBundleMessage.value = notice
      ? `Bundle Docker generado. ${notice}`
      : 'Bundle Docker generado. El panel Docker ya quedó apuntando a ese servicio.'
    return true
  } catch (err) {
    exportBundleError.value = errorText(err, 'No se pudo generar el bundle Docker desde el engine.')
    return false
  } finally {
    exportingBundle.value = false
  }
}

async function loadDockerStatus() {
  dockerLoading.value = true
  dockerError.value = ''
  try {
    const ctx = buildDockerContextPayload()
    const response = await runtimeApi.dockerStatus(ctx.stackName, ctx.composeFile, ctx.envFile)
    dockerStatus.value = response?.data || null

    const stack = String(response?.data?.stackName || '').trim()
    if (stack) dockerStackName.value = stack
    const compose = String(response?.data?.composeFile || '').trim()
    if (compose) dockerComposePath.value = compose
    const envFile = String(response?.data?.envFile || '').trim()
    if (envFile) dockerEnvFilePath.value = envFile

    if (dockerServiceItems.value.length && !dockerSelectedService.value) {
      dockerSelectedService.value = dockerServiceItems.value[0]
    }
  } catch (err) {
    dockerError.value = errorText(err, 'No se pudo obtener el estado Docker.')
  } finally {
    dockerLoading.value = false
  }
}

async function runDockerStackAction(action) {
  const actionKey = String(action || '').toLowerCase()
  if (!['up', 'down', 'restart'].includes(actionKey)) return

  dockerActionLoading.value = actionKey
  dockerError.value = ''
  dockerMessage.value = ''
  try {
    let response
    const ctx = buildDockerContextPayload()
    if (actionKey === 'up') {
      response = await runtimeApi.dockerUp({ ...ctx, build: true, removeOrphans: true })
    } else if (actionKey === 'down') {
      response = await runtimeApi.dockerDown({ ...ctx, removeOrphans: true })
    } else {
      response = await runtimeApi.dockerRestart(ctx)
    }

    const stdout = String(response?.data?.stdout || '').trim()
    dockerMessage.value = stdout ? `Docker ${actionKey}: ${stdout}` : `Docker ${actionKey} ejecutado correctamente.`
    await loadDockerStatus()
  } catch (err) {
    dockerError.value = dockerErrorText(err, `No se pudo ejecutar Docker ${actionKey}.`)
  } finally {
    dockerActionLoading.value = ''
  }
}

async function runDockerServiceAction(action) {
  const service = String(dockerSelectedService.value || '').trim()
  if (!service) return

  const actionKey = String(action || '').toLowerCase()
  if (!['start', 'stop', 'restart'].includes(actionKey)) return

  dockerActionLoading.value = `service_${actionKey}`
  dockerError.value = ''
  dockerMessage.value = ''
  try {
    const ctx = buildDockerContextPayload()
    const response = await runtimeApi.dockerServiceAction(
      service,
      actionKey,
      ctx.stackName,
      ctx.composeFile,
      ctx.envFile
    )
    const stdout = String(response?.data?.stdout || '').trim()
    dockerMessage.value = stdout ? `${service}: ${stdout}` : `${service}: acción ${actionKey} aplicada.`
    await loadDockerStatus()
  } catch (err) {
    dockerError.value = dockerErrorText(err, `No se pudo ejecutar ${actionKey} en ${service}.`)
  } finally {
    dockerActionLoading.value = ''
  }
}

async function loadDockerLogs() {
  dockerActionLoading.value = 'logs'
  dockerError.value = ''
  dockerMessage.value = ''
  try {
    const ctx = buildDockerContextPayload()
    const response = await runtimeApi.dockerLogs({
      stackName: ctx.stackName,
      composeFile: ctx.composeFile,
      envFile: ctx.envFile,
      service: dockerSelectedService.value || null,
      tail: Number(dockerLogsTail.value || 200)
    })
    dockerLogs.value = String(response?.data?.logs || response?.data?.stdout || '').trim()
    if (!dockerLogs.value) {
      dockerMessage.value = 'Sin logs para el filtro actual.'
    }
  } catch (err) {
    dockerError.value = dockerErrorText(err, 'No se pudieron obtener los logs Docker.')
  } finally {
    dockerActionLoading.value = ''
  }
}

async function loadData() {
  loadingData.value = true
  error.value = ''
  try {
    await loadProjects()
    if (!selectedProjectId.value && projects.value.length) {
      selectedProjectId.value = projects.value[0].Id || projects.value[0].id
    }
    await Promise.all([loadWorkflow(selectedProjectId.value), loadRuns(), loadDeployAssets(), loadInferMetrics()])
    suggestDefaultsFromProject()
    if (!dockerStackName.value) {
      dockerStackName.value = String(deployForm.stackName || 'aibase-stack')
    }
    applyDockerContextFromAssets(false)
    await loadDockerStatus()
  } catch (err) {
    error.value = errorText(err, 'No se pudo cargar la etapa Deploy.')
  } finally {
    loadingData.value = false
  }
}

async function triggerDeploy() {
  if (!canRun.value) return

  running.value = true
  runError.value = ''
  runMessage.value = ''

  try {
    const payload = buildRunInputPayload()
    const response = await runtimeApi.triggerProjectRun(
      selectedProjectId.value,
      'deploy_service',
      JSON.stringify(payload)
    )
    const run = normalizeRecord(response?.data || {})
    selectedDeployRunId.value = run.Id || run.id || null
    runMessage.value = 'Deploy ejecutado. Revisa estado y endpoint en el panel de resultado.'
    await Promise.all([loadWorkflow(selectedProjectId.value), loadRuns()])
    await loadDockerStatus()
    await loadInferMetrics()
    await exportDockerBundle()
  } catch (err) {
    runError.value = errorText(err, 'No se pudo ejecutar el deploy.')
  } finally {
    running.value = false
  }
}

async function copyPayloadPreview() {
  try {
    await navigator.clipboard.writeText(payloadPreview.value)
  } catch {
    // no-op
  }
}

async function copyText(value) {
  try {
    await navigator.clipboard.writeText(String(value || ''))
  } catch {
    // no-op
  }
}

function openEndpoint() {
  if (!deployResultSummary.value.endpoint) return
  window.open(deployResultSummary.value.endpoint, '_blank', 'noopener')
}

function openHealth() {
  if (!deployResultSummary.value.health) return
  window.open(deployResultSummary.value.health, '_blank', 'noopener')
}

function goNext() {
  if (!selectedProjectId.value) return
  router.push({ path: '/stage/playground', query: { projectId: selectedProjectId.value } })
}

watch(selectedProjectId, async id => {
  if (!id) {
    runs.value = []
    selectedDeployRunId.value = null
    projectWorkflow.value = null
    deployAssetsData.value = null
    inferMetrics.value = null
    dockerStatus.value = null
    dockerComposePath.value = ''
    dockerEnvFilePath.value = ''
    deployAssetsError.value = ''
    inferMetricsError.value = ''
    exportBundleError.value = ''
    exportBundleMessage.value = ''
    return
  }
  runMessage.value = ''
  runError.value = ''
  exportBundleError.value = ''
  exportBundleMessage.value = ''
  await Promise.all([loadWorkflow(id), loadRuns(), loadDeployAssets(), loadInferMetrics()])
  suggestDefaultsFromProject()
  await loadDockerStatus()
})

watch(() => route.query.projectId, value => {
  if (!value) return
  const asNumber = Number(value)
  selectedProjectId.value = Number.isFinite(asNumber) && asNumber > 0 ? asNumber : value
}, { immediate: true })

watch(dockerServiceItems, items => {
  if (!Array.isArray(items) || !items.length) {
    dockerSelectedService.value = ''
    return
  }
  if (!items.includes(dockerSelectedService.value)) {
    dockerSelectedService.value = items[0]
  }
})

watch(shouldPollRuns, () => {
  ensureRefreshTimer()
}, { immediate: true })

onBeforeUnmount(() => {
  clearRefreshTimer()
})

onMounted(loadData)
</script>

<style scoped>
.stage-page {
  padding-bottom: 30px;
  max-width: 1720px;
  margin: 0 auto;
}

.deploy-main-col,
.deploy-side-col {
  align-self: start;
}

.toggle-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0 10px;
}

.hint-list {
  margin: 0;
  padding-left: 18px;
  display: grid;
  gap: 8px;
}

.hint-list li {
  color: var(--sb-text-soft, var(--sb-muted));
}

.hint-list li.ok {
  color: var(--sb-primary, #2563eb);
  font-weight: 600;
}

.workflow-list {
  display: grid;
  gap: 8px;
}

.workflow-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.deploy-secondary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 16px;
  align-items: start;
}

.deploy-secondary-grid > .card {
  margin-top: 0 !important;
}

.deploy-docker-card {
  grid-column: 1 / -1;
}

.docker-panel {
  display: grid;
  gap: 10px;
}

.docker-overview {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.docker-stack-actions,
.docker-service-actions,
.docker-logs-actions {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
  gap: 8px;
}

.docker-stack-actions :deep(.v-btn),
.docker-service-actions :deep(.v-btn),
.docker-logs-actions :deep(.v-btn) {
  width: 100%;
}

.docker-advanced-panel :deep(.v-expansion-panel-title) {
  min-height: 40px;
  padding-top: 8px;
  padding-bottom: 8px;
  font-size: 0.88rem;
}

.docker-advanced-panel :deep(.v-expansion-panel-text__wrapper) {
  padding-top: 10px;
  padding-bottom: 4px;
}

.docker-ps-list {
  display: grid;
  gap: 8px;
  max-height: 180px;
  overflow: auto;
}

.docker-ps-item {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 10px;
  padding: 8px 10px;
  display: grid;
  gap: 4px;
}

.docker-ps-title {
  font-weight: 600;
}

.docker-ps-meta {
  font-size: 0.8rem;
  color: var(--sb-text-soft, var(--sb-muted));
}

.docker-log-box {
  max-height: 220px;
}

.run-list {
  display: grid;
  gap: 10px;
  max-height: 380px;
  overflow: auto;
}

.run-item {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 12px;
  padding: 10px;
  background: rgba(37, 99, 235, 0.02);
  width: 100%;
  text-align: left;
  cursor: pointer;
}

.run-item.active {
  border-color: rgba(37, 99, 235, 0.5);
  background: rgba(37, 99, 235, 0.08);
}

.run-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.run-meta {
  color: var(--sb-text-soft, var(--sb-muted));
  font-size: 0.84rem;
  margin-top: 4px;
}

.run-error {
  color: var(--sb-danger, #dc2626);
  font-size: 0.84rem;
  margin-top: 6px;
}

.result-meta {
  display: grid;
  gap: 6px;
  font-size: 0.9rem;
}

.preview-box {
  border: 1px solid rgba(120, 140, 170, 0.22);
  border-radius: 12px;
  padding: 10px;
  max-height: 240px;
  overflow: auto;
}

.preview-box pre {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
  font-size: 0.8rem;
}

.command-box pre,
.logs-box pre {
  white-space: pre-wrap;
}

@media (max-width: 960px) {
  .toggle-grid {
    grid-template-columns: 1fr;
  }
}

@media (min-width: 1280px) and (max-width: 1919px) {
  .deploy-secondary-grid {
    grid-template-columns: 1fr;
  }
}

</style>
