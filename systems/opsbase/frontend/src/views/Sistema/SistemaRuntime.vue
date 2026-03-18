<template>
  <v-container fluid :style="themeStyle" :class="['runtime-container', uiMode]">
    <v-row class="mb-4 align-center sb-page-header">
      <v-col>
        <div class="d-flex align-center">
          <div class="sb-page-icon">
            <v-icon color="primary" size="26">mdi-database</v-icon>
          </div>
          <div>
            <h2 class="mb-1">{{ systemTitle }}</h2>
            <span class="sb-page-subtitle text-body-2">
              {{ entidadSeleccionada ? `/${entidadRoute(entidadSeleccionada)}` : '/' }}
            </span>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2">
        <v-btn
          v-if="showAudioRecorder"
          class="cta-button ghost"
          variant="tonal"
          @click="abrirAudioDialog"
        >
          <v-icon left>mdi-microphone</v-icon>
          Grabar audio
        </v-btn>
        <v-btn class="cta-button primary" :disabled="!canCreateRecords" @click="nuevoRegistro">
          <v-icon left>mdi-plus</v-icon>
          Nuevo registro
        </v-btn>
      </v-col>
    </v-row>

    <v-row v-if="showModuleNavigation" class="mb-4">
      <v-col cols="12">
        <v-card elevation="2" class="card module-nav-card">
          <v-card-text class="py-3">
            <div class="module-nav-head">
              <span class="text-caption text-medium-emphasis">Modulo actual</span>
              <strong>{{ currentGroupTitle }}</strong>
            </div>
            <div class="module-nav-entities">
              <v-chip
                v-for="ent in currentGroupEntities"
                :key="ent.entityId || ent.name"
                size="small"
                :color="isCurrentEntity(ent) ? 'primary' : undefined"
                :variant="isCurrentEntity(ent) ? 'flat' : 'tonal'"
                @click="irEntidad(ent)"
              >
                <v-icon start size="16">{{ entidadMenuIcon(ent) }}</v-icon>
                {{ entidadLabel(ent) }}
              </v-chip>
            </div>
            <div class="module-flow">
              <span class="text-caption text-medium-emphasis">Flujo sugerido</span>
              <div class="module-flow-buttons">
                <v-btn
                  v-for="group in flowGroups"
                  :key="group.id"
                  size="x-small"
                  :variant="group.id === currentGroupId ? 'flat' : 'text'"
                  color="primary"
                  @click="irGrupo(group.id)"
                >
                  {{ group.title }}
                </v-btn>
              </div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-row v-if="showOpsFlowPanel" class="mb-4">
      <v-col cols="12">
        <v-card elevation="2" class="card ops-flow-card">
          <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-2">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-transit-transfer</v-icon>
              <span class="text-h6">Flujo operativo rápido</span>
            </div>
            <div class="d-flex ga-2 flex-wrap">
              <v-btn
                v-if="opsRoutes.movement"
                size="small"
                variant="text"
                color="primary"
                @click="irRuta(opsRoutes.movement)"
              >
                Movimientos
              </v-btn>
              <v-btn
                v-if="opsRoutes.movementLine"
                size="small"
                variant="text"
                color="primary"
                @click="irRuta(opsRoutes.movementLine)"
              >
                Líneas
              </v-btn>
              <v-btn
                v-if="opsRoutes.stockBalance"
                size="small"
                variant="text"
                color="primary"
                @click="irRuta(opsRoutes.stockBalance)"
              >
                Stock
              </v-btn>
            </div>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div class="ops-flow-actions">
              <v-btn
                v-for="type in opsQuickTypes"
                :key="type"
                class="cta-button ghost"
                variant="tonal"
                color="primary"
                @click="abrirOperacionGuiada(type)"
              >
                <v-icon left>mdi-plus-circle-outline</v-icon>
                {{ prettifyLabel(type) }}
              </v-btn>
            </div>
            <div class="text-caption text-medium-emphasis mt-3">
              Crea movimiento + línea y opcionalmente confirma en un solo paso.
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-row>
      <v-col cols="12" md="3">
        <v-card elevation="2" class="card side-card summary-card">
          <v-card-title class="d-flex align-center">
            <v-icon class="mr-2" color="primary">mdi-chart-box-outline</v-icon>
            <span class="text-h6">Resumen</span>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="summaryItems.length" class="summary-grid">
              <div v-for="item in summaryItems" :key="item.label" class="summary-item">
                <div class="summary-icon">
                  <v-icon :color="item.color || 'primary'" size="18">{{ item.icon }}</v-icon>
                </div>
                <div>
                  <div class="summary-label">{{ item.label }}</div>
                  <div class="summary-value">{{ item.value }}</div>
                </div>
              </div>
            </div>
            <div v-else class="text-caption text-medium-emphasis">Sin datos para resumir.</div>

            <v-divider v-if="summaryMeta" class="my-3" />
            <div v-if="summaryMeta" class="summary-meta">
              <span class="summary-meta-label">Actualizado:</span>
              <span>{{ summaryMeta }}</span>
            </div>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" md="9">
        <v-card v-if="showIncidentMap" elevation="2" class="card mb-4 map-card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-map</v-icon>
              <span class="text-h6">Mapa</span>
            </div>
            <v-btn
              v-if="mapUrls?.link"
              icon
              variant="text"
              :href="mapUrls.link"
              target="_blank"
              rel="noopener"
              title="Abrir en OpenStreetMap"
            >
              <v-icon>mdi-open-in-new</v-icon>
            </v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="7">
                <div v-if="mapUrls?.embed" class="map-embed">
                  <iframe
                    :src="mapUrls.embed"
                    width="100%"
                    height="300"
                    style="border:0;"
                    loading="lazy"
                    referrerpolicy="no-referrer-when-downgrade"
                  />
                </div>
                <v-alert v-else type="info" variant="tonal">
                  Selecciona un incidente con coordenadas para ver el mapa.
                </v-alert>
              </v-col>
              <v-col cols="12" md="5">
                <div class="text-caption text-medium-emphasis">Lugar</div>
                <div class="mb-2">
                  {{ mapRecord?.LugarNormalizado || mapRecord?.LugarTexto || 'Sin lugar' }}
                </div>
                <div class="text-caption text-medium-emphasis">Descripcion</div>
                <div class="mb-2">
                  {{ mapRecord?.Descripcion || 'Sin descripcion' }}
                </div>
                <div class="text-caption text-medium-emphasis">Fecha/Hora</div>
                <div>
                  {{ formattedCell(mapRecord || {}, { key: 'FechaHora' }).text }}
                </div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>

        <v-card elevation="2" class="card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-table</v-icon>
              <span class="text-h6">{{ entidadTitulo }}</span>
            </div>
            <v-btn icon variant="text" @click="cargarDatos" :disabled="!entidadSeleccionada">
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>
          <v-divider />

          <v-row v-if="showSearch || showFilters" class="px-4 py-2" dense>
            <v-col v-if="showSearch" cols="12" md="4">
              <v-text-field
                v-model="search"
                label="Buscar"
                clearable
                prepend-inner-icon="mdi-magnify"
                :density="uiDensity"
              />
            </v-col>
            <v-col v-if="showFilters" cols="12" md="4">
              <v-select
                v-model="filterField"
                :items="filterFields"
                item-title="title"
                item-value="value"
                label="Filtrar por"
                clearable
                :density="uiDensity"
              />
            </v-col>
            <v-col v-if="showFilters" cols="12" md="4">
              <v-text-field
                v-model="filterValue"
                label="Valor"
                :disabled="!filterField"
                clearable
                :density="uiDensity"
              />
            </v-col>
          </v-row>

          <v-alert v-if="error" type="error" variant="tonal" class="ma-4">
            {{ error }}
          </v-alert>

          <div v-if="loading" class="pa-4">
            <v-skeleton-loader type="heading, text, table" class="sb-skeleton" />
          </div>

          <div v-else-if="!entidadSeleccionada" class="pa-4">
            Selecciona una vista para ver registros.
          </div>

          <v-data-table
            v-else
            :headers="headers"
            :items="paginatedRegistros"
            class="table"
            :density="uiDensity"
            :fixed-header="listStickyHeader"
            :height="listStickyHeader ? 420 : undefined"
            :no-data-text="entityMessages.empty"
            hover
          >
            <template #item="{ item, columns }">
              <tr
                :class="{ 'row-selected': showIncidentMap && mapRecord && getRecordId(item.raw || item) === getRecordId(mapRecord) }"
                @click="showIncidentMap ? (mapRecord = item.raw || item) : null"
              >
                <td v-for="col in columns" :key="col.key" :class="{ 'actions-td': col.key === 'actions' }">
                  <template v-if="col.key === 'actions'">
                    <div class="actions-cell actions-grid">
                      <v-tooltip text="Editar">
                        <template #activator="{ props }">
                          <v-btn
                            v-bind="props"
                            icon
                            size="x-small"
                            color="primary"
                            variant="text"
                            :disabled="!canEditItem(item.raw || item)"
                            @click.stop="editarRegistro(item.raw || item)"
                          >
                            <v-icon>mdi-pencil</v-icon>
                          </v-btn>
                        </template>
                      </v-tooltip>
                      <v-tooltip v-if="canConfirmMovement(item.raw || item)" text="Confirmar movimiento">
                        <template #activator="{ props }">
                          <v-btn
                            v-bind="props"
                            icon
                            size="x-small"
                            color="green"
                            variant="text"
                            :loading="isConfirmingMovement(item.raw || item)"
                            :disabled="isConfirmingMovement(item.raw || item)"
                            @click.stop="confirmarMovimiento(item.raw || item)"
                          >
                            <v-icon>mdi-check-circle</v-icon>
                          </v-btn>
                        </template>
                      </v-tooltip>
                      <v-tooltip v-if="enableDuplicate" text="Duplicar">
                        <template #activator="{ props }">
                          <v-btn
                            v-bind="props"
                            icon
                            size="x-small"
                            color="blue"
                            variant="text"
                            :disabled="!canDuplicateItem(item.raw || item)"
                            @click.stop="duplicarRegistro(item.raw || item)"
                          >
                            <v-icon>mdi-content-copy</v-icon>
                          </v-btn>
                        </template>
                      </v-tooltip>
                      <v-tooltip text="Copiar datos">
                        <template #activator="{ props }">
                          <v-btn v-bind="props" icon size="x-small" color="indigo" variant="text" @click.stop="copiarRegistro(item.raw || item)">
                            <v-icon>mdi-clipboard-text-outline</v-icon>
                          </v-btn>
                        </template>
                      </v-tooltip>
                      <v-tooltip v-if="showTraceAction" text="Ver trazabilidad">
                        <template #activator="{ props }">
                          <v-btn
                            v-bind="props"
                            icon
                            size="x-small"
                            color="teal"
                            variant="text"
                            @click.stop="abrirTrazabilidad(item.raw || item)"
                          >
                            <v-icon>mdi-timeline-clock-outline</v-icon>
                          </v-btn>
                        </template>
                      </v-tooltip>
                      <v-tooltip v-if="showRetryJob" text="Reintentar job">
                        <template #activator="{ props }">
                          <v-btn
                            v-bind="props"
                            icon
                            size="x-small"
                            color="orange"
                            variant="text"
                            :loading="isRetrying(item.raw || item)"
                            :disabled="isRetrying(item.raw || item)"
                            @click.stop="reintentarJob(item.raw || item)"
                          >
                            <v-icon>mdi-reload</v-icon>
                          </v-btn>
                        </template>
                      </v-tooltip>
                      <v-tooltip v-if="showMapAction" text="Ver en mapa">
                        <template #activator="{ props }">
                          <v-btn
                            v-bind="props"
                            icon
                            size="x-small"
                            color="green"
                            variant="text"
                            :disabled="!hasCoords(item.raw || item)"
                            @click.stop="abrirMapa(item.raw || item)"
                          >
                            <v-icon>mdi-map-marker</v-icon>
                          </v-btn>
                        </template>
                      </v-tooltip>
                      <v-tooltip v-if="showAudioPlayAction" text="Escuchar audio">
                        <template #activator="{ props }">
                          <v-btn
                            v-bind="props"
                            icon
                            size="x-small"
                            color="deep-purple"
                            variant="text"
                            :disabled="!hasAudioFile(item.raw || item)"
                            @click.stop="abrirAudioPlayback(item.raw || item)"
                          >
                            <v-icon>mdi-play-circle</v-icon>
                          </v-btn>
                        </template>
                      </v-tooltip>
                      <v-tooltip v-if="quickToggleField" :text="`Toggle ${quickToggleField.label || quickToggleField.name}`">
                        <template #activator="{ props }">
                          <v-btn v-bind="props" icon size="x-small" color="teal" variant="text" @click.stop="toggleQuickField(item.raw || item)">
                            <v-icon>mdi-toggle-switch</v-icon>
                          </v-btn>
                        </template>
                      </v-tooltip>
                      <v-tooltip text="Eliminar">
                        <template #activator="{ props }">
                          <v-btn
                            v-bind="props"
                            icon
                            size="x-small"
                            color="red"
                            variant="text"
                            :disabled="!canDeleteItem(item.raw || item)"
                            @click.stop="eliminarRegistro(item.raw || item)"
                          >
                            <v-icon>mdi-delete</v-icon>
                          </v-btn>
                        </template>
                      </v-tooltip>
                    </div>
                  </template>
                  <template v-else>
                    <template v-if="formattedCell(item.raw || item, col).isChip">
                      <v-chip size="small" :color="formattedCell(item.raw || item, col).color">
                        {{ formattedCell(item.raw || item, col).text }}
                      </v-chip>
                    </template>
                    <template v-else-if="shouldShowProgress(item.raw || item, col)">
                      <div class="d-flex flex-column">
                        <span class="text-caption">{{ formattedCell(item.raw || item, col).text }}</span>
                        <v-progress-linear indeterminate color="orange" height="4" class="mt-1" />
                      </div>
                    </template>
                    <template v-else>
                      <span
                        class="cell-text"
                        :title="formattedCell(item.raw || item, col).text"
                      >
                        {{ formattedCell(item.raw || item, col).text }}
                      </span>
                    </template>
                  </template>
                </td>
              </tr>
            </template>
          </v-data-table>

          <div v-if="listShowTotals" class="px-4 pt-2 text-caption text-medium-emphasis">
            Total: {{ sortedRegistros.length }} registros
          </div>

          <v-row class="px-4 pb-4 pt-2 align-center" dense>
            <v-col cols="12" md="4">
              <v-select
                v-model="itemsPerPage"
                :items="itemsPerPageOptions"
                label="Filas por pagina"
                :density="uiDensity"
              />
            </v-col>
            <v-col cols="12" md="8" class="d-flex justify-end">
              <v-pagination
                v-model="page"
                :length="pageCount"
                density="compact"
              />
            </v-col>
          </v-row>
        </v-card>
      </v-col>
    </v-row>

    <RegistroDialog
      v-model="dialog"
      :record="registroActual"
      :fields="campos"
      :layout="formLayout"
      :density="uiDensity"
      :messages="entityMessages"
      :confirm-save="confirmSave"
      :mode="dialogMode"
      :api-route="apiRoute"
      @guardado="cargarDatos"
    />

    <v-dialog v-model="opsDialog" max-width="760">
      <v-card class="sb-dialog">
        <v-card-title class="sb-dialog-title">
          <div class="sb-dialog-icon">
            <v-icon color="primary">mdi-transit-transfer</v-icon>
          </div>
          <div>
            <div class="sb-dialog-title-text">Nueva operación logística</div>
            <div class="sb-dialog-subtitle">Crea movimiento, línea y confirmación opcional.</div>
          </div>
        </v-card-title>
        <v-divider />
        <v-card-text class="sb-dialog-body">
          <v-alert v-if="opsError" type="error" variant="tonal" class="mb-3">
            {{ opsError }}
          </v-alert>

          <v-row dense>
            <v-col cols="12" md="6">
              <v-select
                v-model="opsForm.movementType"
                :items="opsMovementTypeItems"
                item-title="title"
                item-value="value"
                label="Tipo de movimiento"
                :density="uiDensity"
                variant="outlined"
              />
            </v-col>
            <v-col cols="12" md="6">
              <v-text-field
                v-model="opsForm.referenceNo"
                label="Referencia"
                :density="uiDensity"
                variant="outlined"
                hint="Opcional; si no se completa se genera automáticamente."
                persistent-hint
              />
            </v-col>

            <v-col cols="12" md="6">
              <v-select
                v-model="opsForm.sourceLocationId"
                :items="opsLocationItems"
                item-title="title"
                item-value="value"
                label="Ubicación origen"
                :density="uiDensity"
                variant="outlined"
                :disabled="opsDisableSource"
                clearable
              />
            </v-col>
            <v-col cols="12" md="6">
              <v-select
                v-model="opsForm.targetLocationId"
                :items="opsLocationItems"
                item-title="title"
                item-value="value"
                label="Ubicación destino"
                :density="uiDensity"
                variant="outlined"
                :disabled="opsDisableTarget"
                clearable
              />
            </v-col>

            <v-col cols="12" md="6">
              <v-select
                v-model="opsForm.resourceInstanceId"
                :items="opsResourceItems"
                item-title="title"
                item-value="value"
                label="Recurso"
                :density="uiDensity"
                variant="outlined"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field
                v-model.number="opsForm.quantity"
                type="number"
                min="0.001"
                step="0.001"
                label="Cantidad"
                :density="uiDensity"
                variant="outlined"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field
                v-model.number="opsForm.unitCost"
                type="number"
                min="0"
                step="0.01"
                label="Costo unitario"
                :density="uiDensity"
                variant="outlined"
                clearable
              />
            </v-col>

            <v-col cols="12">
              <v-textarea
                v-model="opsForm.notes"
                label="Notas"
                :density="uiDensity"
                variant="outlined"
                rows="2"
                auto-grow
              />
            </v-col>
            <v-col cols="12">
              <v-checkbox
                v-model="opsForm.autoConfirm"
                :density="uiDensity"
                label="Confirmar automáticamente al guardar"
                hide-details
              />
            </v-col>
          </v-row>
        </v-card-text>
        <v-divider />
        <v-card-actions class="sb-dialog-actions d-flex justify-end ga-2">
          <v-btn class="sb-btn ghost" variant="text" @click="opsDialog = false">Cancelar</v-btn>
          <v-btn class="sb-btn primary" color="primary" :loading="opsSubmitting" @click="guardarOperacionGuiada">
            Ejecutar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="audioDialog" max-width="640">
      <v-card class="sb-dialog">
        <v-card-title class="sb-dialog-title">
          <div class="sb-dialog-icon">
            <v-icon color="deep-purple">mdi-microphone</v-icon>
          </div>
          <div>
            <div class="sb-dialog-title-text">Grabar audio</div>
            <div class="sb-dialog-subtitle">Captura directa desde el navegador.</div>
          </div>
        </v-card-title>
        <v-divider />
        <v-card-text class="sb-dialog-body">
          <v-alert v-if="!audioSupported" type="error" variant="tonal" class="mb-4">
            Tu navegador no soporta grabacion de audio.
          </v-alert>

          <v-text-field
            v-model="audioDescripcion"
            label="Descripcion"
            hint="Opcional: agrega contexto del incidente"
            persistent-hint
            :density="uiDensity"
            variant="outlined"
          />

          <v-row class="mt-2" dense>
            <v-col cols="12" sm="4">
              <v-btn
                class="sb-btn danger"
                block
                variant="tonal"
                :disabled="audioRecording || !audioSupported"
                @click="startRecording"
              >
                <v-icon left>mdi-record-circle</v-icon>
                Grabar
              </v-btn>
            </v-col>
            <v-col cols="12" sm="4">
              <v-btn
                class="sb-btn warning"
                block
                variant="tonal"
                :disabled="!audioRecording"
                @click="stopRecording"
              >
                <v-icon left>mdi-stop</v-icon>
                Detener
              </v-btn>
            </v-col>
            <v-col cols="12" sm="4">
              <v-btn class="sb-btn ghost" block variant="text" :disabled="audioRecording" @click="clearRecording">
                Limpiar
              </v-btn>
            </v-col>
          </v-row>

          <audio v-if="audioUrl" class="audio-player mt-4" controls :src="audioUrl"></audio>

          <v-alert v-if="audioError" type="error" variant="tonal" class="mt-4">
            {{ audioError }}
          </v-alert>

          <v-alert v-if="audioSuccess" type="success" variant="tonal" class="mt-4">
            Audio enviado. Job #{{ audioJobId }}
          </v-alert>
          <v-alert v-if="audioProcessing" type="info" variant="tonal" class="mt-4">
            Procesando audio... Estado: {{ audioJobStatus || 'pendiente' }}
            <div v-if="audioJobLastError" class="text-caption mt-1">
              {{ audioJobLastError }}
            </div>
            <v-progress-linear
              class="mt-2"
              indeterminate
              color="deep-purple"
              height="4"
            />
          </v-alert>
        </v-card-text>
        <v-divider />
        <v-card-actions class="d-flex justify-end ga-2 sb-dialog-actions">
          <v-btn class="sb-btn ghost" variant="text" @click="cerrarAudioDialog">Cerrar</v-btn>
          <v-btn
            class="sb-btn primary"
            color="deep-purple"
            :disabled="!audioBlob || audioUploading"
            @click="uploadRecording"
          >
            <v-icon left>mdi-cloud-upload</v-icon>
            Enviar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="audioPlayDialog" max-width="520">
      <v-card class="sb-dialog">
        <v-card-title class="sb-dialog-title">
          <div class="sb-dialog-icon">
            <v-icon color="deep-purple">mdi-play-circle</v-icon>
          </div>
          <div>
            <div class="sb-dialog-title-text">Reproducir audio</div>
            <div class="sb-dialog-subtitle">Escucha el archivo original.</div>
          </div>
        </v-card-title>
        <v-divider />
        <v-card-text class="sb-dialog-body">
          <div v-if="audioPlayItem" class="text-caption text-medium-emphasis mb-2">
            Audio #{{ getRecordId(audioPlayItem) }}
            <span v-if="audioPlayItem?.Incidenteid || audioPlayItem?.IncidenteId">
              · Incidente {{ audioPlayItem?.Incidenteid || audioPlayItem?.IncidenteId }}
            </span>
          </div>
          <v-progress-linear v-if="audioPlayLoading" indeterminate color="deep-purple" height="4" class="mb-3" />
          <v-alert v-if="audioPlayError" type="error" variant="tonal" class="mb-3">
            {{ audioPlayError }}
          </v-alert>
          <audio
            v-if="audioPlayUrl"
            :key="audioPlayUrl"
            class="audio-player"
            controls
            preload="auto"
            @error="onAudioPlayError"
          >
            <source :src="audioPlayUrl" :type="audioPlayMime || undefined" />
          </audio>
        </v-card-text>
        <v-divider />
        <v-card-actions class="d-flex justify-end sb-dialog-actions">
          <v-btn class="sb-btn ghost" variant="text" @click="cerrarAudioPlayback">Cerrar</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="traceDialog" max-width="860">
      <v-card class="sb-dialog">
        <v-card-title class="sb-dialog-title">
          <div class="sb-dialog-icon">
            <v-icon color="teal">mdi-timeline-clock-outline</v-icon>
          </div>
          <div>
            <div class="sb-dialog-title-text">Trazabilidad del recurso</div>
            <div class="sb-dialog-subtitle">
              {{ traceResourceLabel || 'Historial operacional' }}
            </div>
          </div>
        </v-card-title>
        <v-divider />
        <v-card-text class="sb-dialog-body">
          <v-alert v-if="traceError" type="error" variant="tonal" class="mb-3">
            {{ traceError }}
          </v-alert>
          <div v-if="traceLoading" class="sb-skeleton" style="height: 120px;"></div>
          <div v-else-if="!traceItems.length" class="text-caption text-medium-emphasis">
            Sin eventos para este recurso.
          </div>
          <div v-else class="trace-list">
            <div v-for="evt in traceItems" :key="evt.id" class="trace-item">
              <div class="trace-item-head">
                <v-chip size="small" :color="statusChipColor(evt.result)">
                  {{ evt.result || 'evento' }}
                </v-chip>
                <strong>{{ evt.operationName }}</strong>
                <span class="text-caption text-medium-emphasis">
                  {{ evt.executedAt }}
                </span>
              </div>
              <div class="trace-item-meta">
                {{ evt.entityName }}#{{ evt.entityId ?? '-' }} · {{ evt.actor || 'system' }}
              </div>
              <div v-if="evt.message" class="trace-item-message">
                {{ evt.message }}
              </div>
            </div>
          </div>
        </v-card-text>
        <v-divider />
        <v-card-actions class="d-flex justify-end sb-dialog-actions">
          <v-btn class="sb-btn ghost" variant="text" @click="traceDialog = false">Cerrar</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-snackbar v-model="toastOpen" :timeout="2200" :color="toastColor">
      {{ toastMessage }}
    </v-snackbar>
  </v-container>
</template>

<script setup>
import { computed, inject, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import frontendConfig from '../../config/frontend-config.json'
import { toKebab } from '../../utils/slug.js'
import RegistroDialog from '../../components/sistemas/RegistroDialog.vue'
import runtimeApi from '../../api/runtime.service.js'

const route = useRoute()
const router = useRouter()
const colorMode = inject('colorMode', null)
const isDark = computed(() => {
  if (colorMode?.isDark?.value != null) return colorMode.isDark.value
  if (typeof localStorage !== 'undefined') {
    return localStorage.getItem('sb-theme') === 'dark'
  }
  return false
})

const config = ref(JSON.parse(JSON.stringify(frontendConfig || {})))
const OPS_MOVEMENT_TYPES = ['ingreso', 'egreso', 'transferencia', 'ajuste', 'reserva', 'liberacion']
const OPS_QUICK_TYPES = ['ingreso', 'transferencia', 'egreso', 'reserva', 'liberacion']
const OPS_MOVEMENT_STATUSES = ['borrador', 'confirmado', 'anulado']
const OPS_TRACK_MODES = ['none', 'serial', 'lote', 'serial_lote']
const OPS_RESOURCE_STATES = ['activo', 'inactivo', 'bloqueado', 'cuarentena', 'reparacion', 'baja']
const OPS_LOCATION_TYPES = ['nodo', 'deposito', 'sector', 'pasillo', 'estanteria', 'nivel', 'posicion']
const OPS_ATTRIBUTE_TYPES = ['string', 'int', 'decimal', 'bool', 'datetime', 'json']
const OPS_AUDIT_RESULTS = ['ok', 'error', 'warning']
const RELATION_ROUTES = {
  movement: 'movement',
  resourceinstance: 'resource-instance',
  location: 'location',
  resourcedefinition: 'resource-definition',
  attributedefinition: 'attribute-definition'
}
const OPS_GROUPS = [
  { id: 'catalogo', title: 'Catalogo' },
  { id: 'ubicaciones', title: 'Ubicaciones' },
  { id: 'operaciones', title: 'Operaciones' },
  { id: 'stock', title: 'Stock' },
  { id: 'control', title: 'Control' },
  { id: 'otros', title: 'Otros' }
]
const OPS_ENTITY_LABELS = {
  attributedefinition: 'Definiciones de atributos',
  attributevalue: 'Valores de atributos',
  location: 'Ubicaciones',
  movement: 'Movimientos',
  movementline: 'Lineas de movimiento',
  operationaudit: 'Auditoria operativa',
  resourcedefinition: 'Definiciones de recursos',
  resourceinstance: 'Instancias de recursos',
  stockbalance: 'Saldos de stock'
}
const OPS_ENTITY_ICONS = {
  attributedefinition: 'mdi-tag-outline',
  attributevalue: 'mdi-form-textbox',
  location: 'mdi-map-marker-radius-outline',
  movement: 'mdi-swap-horizontal-bold',
  movementline: 'mdi-format-list-bulleted',
  operationaudit: 'mdi-timeline-alert-outline',
  resourcedefinition: 'mdi-shape-plus-outline',
  resourceinstance: 'mdi-cube-outline',
  stockbalance: 'mdi-scale-balance'
}
const OPS_ENTITY_GROUPS = {
  resourcedefinition: 'catalogo',
  attributedefinition: 'catalogo',
  attributevalue: 'catalogo',
  resourceinstance: 'catalogo',
  location: 'ubicaciones',
  movement: 'operaciones',
  movementline: 'operaciones',
  stockbalance: 'stock',
  operationaudit: 'control'
}
const OPS_ENTITY_ORDER = {
  resourcedefinition: 10,
  attributedefinition: 20,
  attributevalue: 30,
  resourceinstance: 40,
  location: 50,
  movement: 60,
  movementline: 70,
  stockbalance: 80,
  operationaudit: 90
}
const OPS_SECTIONED_ENTITY_KEYS = new Set([
  'resourcedefinition',
  'attributedefinition',
  'attributevalue',
  'resourceinstance',
  'location',
  'movement',
  'movementline',
  'stockbalance',
  'operationaudit'
])
const OPS_FIELD_LAYOUT = {
  movement: {
    movementtype: { section: 'Operacion', sectionOrder: 10, formOrder: 10 },
    status: { section: 'Operacion', sectionOrder: 10, formOrder: 20 },
    referenceno: { section: 'Operacion', sectionOrder: 10, formOrder: 30 },
    notes: { section: 'Operacion', sectionOrder: 10, formOrder: 40 },
    operationat: { section: 'Operacion', sectionOrder: 10, formOrder: 50 },
    sourcelocationid: { section: 'Ubicaciones', sectionOrder: 20, formOrder: 10 },
    targetlocationid: { section: 'Ubicaciones', sectionOrder: 20, formOrder: 20 },
    createdby: { section: 'Control', sectionOrder: 30, formOrder: 10 }
  },
  movementline: {
    movementid: { section: 'Referencias', sectionOrder: 10, formOrder: 10 },
    resourceinstanceid: { section: 'Referencias', sectionOrder: 10, formOrder: 20 },
    quantity: { section: 'Cantidades', sectionOrder: 20, formOrder: 10 },
    unitcost: { section: 'Cantidades', sectionOrder: 20, formOrder: 20 },
    serie: { section: 'Trazabilidad', sectionOrder: 30, formOrder: 10 },
    lote: { section: 'Trazabilidad', sectionOrder: 30, formOrder: 20 }
  },
  resourcedefinition: {
    codigo: { section: 'Identidad', sectionOrder: 10, formOrder: 10 },
    nombre: { section: 'Identidad', sectionOrder: 10, formOrder: 20 },
    descripcion: { section: 'Identidad', sectionOrder: 10, formOrder: 30 },
    trackmode: { section: 'Seguimiento', sectionOrder: 20, formOrder: 10 },
    isactive: { section: 'Estado', sectionOrder: 30, formOrder: 10 }
  },
  resourceinstance: {
    resourcedefinitionid: { section: 'Identidad', sectionOrder: 10, formOrder: 10 },
    codigointerno: { section: 'Identidad', sectionOrder: 10, formOrder: 20 },
    estado: { section: 'Estado', sectionOrder: 20, formOrder: 10 },
    isactive: { section: 'Estado', sectionOrder: 20, formOrder: 20 },
    serie: { section: 'Trazabilidad', sectionOrder: 30, formOrder: 10 },
    lote: { section: 'Trazabilidad', sectionOrder: 30, formOrder: 20 },
    vencimiento: { section: 'Trazabilidad', sectionOrder: 30, formOrder: 30 }
  },
  location: {
    codigo: { section: 'Identidad', sectionOrder: 10, formOrder: 10 },
    nombre: { section: 'Identidad', sectionOrder: 10, formOrder: 20 },
    tipo: { section: 'Identidad', sectionOrder: 10, formOrder: 30 },
    parentlocationid: { section: 'Jerarquia', sectionOrder: 20, formOrder: 10 },
    capacidad: { section: 'Capacidad', sectionOrder: 30, formOrder: 10 },
    isactive: { section: 'Estado', sectionOrder: 40, formOrder: 10 }
  },
  stockbalance: {
    resourceinstanceid: { section: 'Referencias', sectionOrder: 10, formOrder: 10 },
    locationid: { section: 'Referencias', sectionOrder: 10, formOrder: 20 },
    stockreal: { section: 'Saldos', sectionOrder: 20, formOrder: 10 },
    stockreservado: { section: 'Saldos', sectionOrder: 20, formOrder: 20 },
    stockdisponible: { section: 'Saldos', sectionOrder: 20, formOrder: 30 }
  },
  attributedefinition: {
    resourcedefinitionid: { section: 'Referencias', sectionOrder: 10, formOrder: 10 },
    codigo: { section: 'Definicion', sectionOrder: 20, formOrder: 10 },
    nombre: { section: 'Definicion', sectionOrder: 20, formOrder: 20 },
    datatype: { section: 'Definicion', sectionOrder: 20, formOrder: 30 },
    isrequired: { section: 'Reglas', sectionOrder: 30, formOrder: 10 },
    maxlength: { section: 'Reglas', sectionOrder: 30, formOrder: 20 },
    sortorder: { section: 'Reglas', sectionOrder: 30, formOrder: 30 },
    isactive: { section: 'Estado', sectionOrder: 40, formOrder: 10 }
  },
  attributevalue: {
    resourceinstanceid: { section: 'Referencias', sectionOrder: 10, formOrder: 10 },
    attributedefinitionid: { section: 'Referencias', sectionOrder: 10, formOrder: 20 },
    valuestring: { section: 'Valor', sectionOrder: 20, formOrder: 10 },
    valuenumber: { section: 'Valor', sectionOrder: 20, formOrder: 20 },
    valuedatetime: { section: 'Valor', sectionOrder: 20, formOrder: 30 },
    valuebool: { section: 'Valor', sectionOrder: 20, formOrder: 40 },
    valuejson: { section: 'Valor', sectionOrder: 20, formOrder: 50 }
  },
  operationaudit: {
    operationname: { section: 'Evento', sectionOrder: 10, formOrder: 10 },
    entityname: { section: 'Contexto', sectionOrder: 20, formOrder: 10 },
    entityid: { section: 'Contexto', sectionOrder: 20, formOrder: 20 },
    actor: { section: 'Contexto', sectionOrder: 20, formOrder: 30 },
    result: { section: 'Resultado', sectionOrder: 30, formOrder: 10 },
    message: { section: 'Resultado', sectionOrder: 30, formOrder: 20 }
  }
}

const registros = ref([])
const loading = ref(false)
const error = ref('')

const search = ref('')
const filterField = ref(null)
const filterValue = ref('')

const page = ref(1)
const itemsPerPage = ref(10)

const dialog = ref(false)
const dialogMode = ref('create')
const registroActual = ref(null)
const opsDialog = ref(false)
const opsSubmitting = ref(false)
const opsError = ref('')
const opsForm = ref(buildOpsForm())
const opsLocationItems = ref([])
const opsResourceItems = ref([])

const audioDialog = ref(false)
const audioRecording = ref(false)
const audioUploading = ref(false)
const audioProcessing = ref(false)
const audioJobStatus = ref('')
const audioJobLastError = ref('')
const audioError = ref('')
const audioSuccess = ref(false)
const audioJobId = ref(null)
const audioDescripcion = ref('')
const audioBlob = ref(null)
const audioUrl = ref('')
const audioMime = ref('')
const audioPlayDialog = ref(false)
const audioPlayLoading = ref(false)
const audioPlayError = ref('')
const audioPlayUrl = ref('')
const audioPlayMime = ref('')
const audioPlayItem = ref(null)
const traceDialog = ref(false)
const traceLoading = ref(false)
const traceError = ref('')
const traceItems = ref([])
const traceResourceLabel = ref('')
const retryingIds = ref({})
const confirmingIds = ref({})
const movementStatusById = ref({})
const relationLookups = ref({
  movement: {},
  resourceinstance: {},
  location: {},
  resourcedefinition: {},
  attributedefinition: {}
})
const permissionSet = ref(new Set())
const permissionsLoaded = ref(false)
const permissionsBypass = ref(false)
const mapRecord = ref(null)
const autoRefreshIntervalMs = computed(() => config.value?.system?.autoRefreshIntervalMs || 3000)
const autoRefreshAlways = computed(() => config.value?.system?.autoRefreshAlways === true)
const hasActiveProcessing = computed(() => {
  if (!registros.value.length) return false
  const active = new Set(['processing', 'pending', 'retry', 'queued', 'running'])
  return registros.value.some(item => {
    const raw = item || {}
    const status = (raw.Status ?? raw.status ?? raw.Step ?? raw.step ?? raw.Estado ?? raw.estado ?? '').toString().toLowerCase()
    return active.has(status)
  })
})
const autoRefreshEnabled = computed(() => {
  if (!entidadSeleccionada.value) return false
  if (entidadSeleccionada.value.autoRefresh === false) return false
  if (autoRefreshAlways.value) return true
  if (audioProcessing.value) return true
  if (Object.keys(retryingIds.value).length > 0) return true
  return hasActiveProcessing.value
})
let autoRefreshTimer = null
let autoRefreshInFlight = false
let mediaRecorder = null
let mediaStream = null
let audioChunks = []
let audioPollTimer = null
let audioPollInFlight = false

const toastOpen = ref(false)
const toastMessage = ref('')
const toastColor = ref('green')

const entidadSeleccionada = ref(null)

const systemTitle = computed(() => config.value?.system?.appTitle || 'Sistema')

const uiDensity = computed(() => config.value?.system?.density || 'comfortable')
const uiMode = computed(() => config.value?.system?.uiMode || 'enterprise')
const locale = computed(() => config.value?.system?.locale || 'es-AR')
const currency = computed(() => config.value?.system?.currency || 'ARS')

const lightThemeDefaults = {
  primary: '#1d4ed8',
  secondary: '#0ea5e9',
  accent: '#f97316',
  primarySoft: 'rgba(29,78,216,0.12)',
  background: '#f8fafc',
  surface: '#ffffff',
  muted: '#64748b',
  border: 'rgba(15,23,42,0.12)',
  borderSoft: 'rgba(15,23,42,0.08)',
  radius: 16,
  shadow: '0 12px 30px rgba(15, 23, 42, 0.12)',
  fontBody: "Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif",
  fontDisplay: "'Space Grotesk', Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif",
  gradient: 'linear-gradient(135deg, rgba(29,78,216,0.16), rgba(14,165,233,0.08) 45%, rgba(248,250,252,0.95))',
  patternOpacity: 0.06,
  headerBg: 'rgba(255,255,255,0.9)',
  text: '#0f172a',
  textSoft: '#334155'
}

const darkThemeDefaults = {
  primary: '#1d4ed8',
  secondary: '#0ea5e9',
  accent: '#f97316',
  primarySoft: 'rgba(59,130,246,0.18)',
  background: '#0b1120',
  surface: '#0f172a',
  muted: '#94a3b8',
  border: 'rgba(148,163,184,0.28)',
  borderSoft: 'rgba(148,163,184,0.16)',
  radius: 16,
  shadow: '0 12px 30px rgba(15, 23, 42, 0.35)',
  fontBody: "Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif",
  fontDisplay: "'Space Grotesk', Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif",
  gradient: 'linear-gradient(135deg, rgba(59,130,246,0.18), rgba(15,23,42,0.9) 55%)',
  patternOpacity: 0.12,
  headerBg: 'rgba(15,23,42,0.85)',
  text: '#e2e8f0',
  textSoft: '#94a3b8'
}

const themeStyle = computed(() => {
  const system = config.value?.system || {}
  const theme = config.value?.theme || {}
  const themeDark = config.value?.themeDark || {}
  const defaults = isDark.value ? darkThemeDefaults : lightThemeDefaults
  const activeTheme = isDark.value ? themeDark : theme
  const baseBrand = theme?.brand || {}
  const darkBrand = themeDark?.brand || {}
  const brand = isDark.value ? { ...baseBrand, ...darkBrand } : baseBrand
  return {
    '--sb-primary': brand.primary || system.primaryColor || defaults.primary,
    '--sb-secondary': brand.secondary || system.secondaryColor || defaults.secondary,
    '--sb-accent': brand.accent || defaults.accent,
    '--sb-primary-soft': activeTheme.primarySoft || defaults.primarySoft,
    '--sb-bg': activeTheme.background || defaults.background,
    '--sb-surface': activeTheme.surface || defaults.surface,
    '--sb-muted': activeTheme.muted || defaults.muted,
    '--sb-border': activeTheme.border || defaults.border,
    '--sb-border-soft': activeTheme.borderSoft || defaults.borderSoft,
    '--sb-radius': `${activeTheme.radius ?? theme.radius ?? defaults.radius}px`,
    '--sb-shadow': activeTheme.shadow || defaults.shadow,
    '--sb-font': activeTheme.fontBody || theme.fontBody || system.fontFamily || defaults.fontBody,
    '--sb-font-display': activeTheme.fontDisplay || theme.fontDisplay || defaults.fontDisplay,
    '--sb-gradient': activeTheme.gradient || defaults.gradient,
    '--sb-pattern-opacity': activeTheme.patternOpacity ?? defaults.patternOpacity,
    '--sb-header-bg': activeTheme.headerBg || defaults.headerBg,
    '--sb-text': activeTheme.text || defaults.text,
    '--sb-text-soft': activeTheme.textSoft || defaults.textSoft
  }
})

const entities = computed(() => config.value?.entities || [])

const runtimeEntities = computed(() => entities.value.filter(entity => entity.showInMenu !== false))

function entityRouteKey(entity) {
  return entity?.routeSlug || entity?.name || entity?.menuLabel || ''
}

function entityCanonicalKey(entity) {
  return normalizeRouteKey(entityRouteKey(entity))
}

function entidadRoute(entidad) {
  return toKebab(entityRouteKey(entidad) || 'item')
}

function entidadLabel(entidad) {
  const key = entityCanonicalKey(entidad)
  return OPS_ENTITY_LABELS[key] || entidad?.menuLabel || entidad?.displayName || entidad?.name || 'Entidad'
}

function entidadMenuIcon(entidad) {
  const key = entityCanonicalKey(entidad)
  return OPS_ENTITY_ICONS[key] || entidad?.menuIcon || 'mdi-table'
}

const entitySlug = computed(() => route.params.entity || '')

const groupedRuntimeEntities = computed(() => {
  const grouped = {}
  OPS_GROUPS.forEach(group => { grouped[group.id] = [] })
  runtimeEntities.value.forEach(entity => {
    const key = entityCanonicalKey(entity)
    const groupId = OPS_ENTITY_GROUPS[key] || 'otros'
    if (!grouped[groupId]) grouped[groupId] = []
    grouped[groupId].push(entity)
  })
  Object.keys(grouped).forEach(groupId => {
    grouped[groupId].sort((a, b) => {
      const orderA = OPS_ENTITY_ORDER[entityCanonicalKey(a)] ?? 999
      const orderB = OPS_ENTITY_ORDER[entityCanonicalKey(b)] ?? 999
      if (orderA !== orderB) return orderA - orderB
      return entidadLabel(a).localeCompare(entidadLabel(b), 'es')
    })
  })
  return grouped
})

const entidadTitulo = computed(() => entidadSeleccionada.value ? entidadLabel(entidadSeleccionada.value) : 'Entidad')
const currentEntityCanonicalKey = computed(() => entityCanonicalKey(entidadSeleccionada.value))
const currentGroupId = computed(() => OPS_ENTITY_GROUPS[currentEntityCanonicalKey.value] || 'otros')
const currentGroupTitle = computed(() => OPS_GROUPS.find(group => group.id === currentGroupId.value)?.title || 'Otros')
const currentGroupEntities = computed(() => groupedRuntimeEntities.value[currentGroupId.value] || [])
const flowGroups = computed(() => OPS_GROUPS
  .filter(group => group.id !== 'otros')
  .filter(group => (groupedRuntimeEntities.value[group.id] || []).length > 0))
const showModuleNavigation = computed(() => hasOpsCore.value && Boolean(entidadSeleccionada.value))

const campos = computed(() => entidadSeleccionada.value?.fields || [])

const listFields = computed(() => campos.value.filter(field => field.showInList !== false))

const pkField = computed(() => {
  return campos.value.find(f => f.isPrimaryKey) || campos.value.find(f => String(f.columnName || f.name).toLowerCase() === 'id')
})

const quickToggleField = computed(() => campos.value.find(f => f.quickToggle))

const entityMessages = computed(() => entidadSeleccionada.value?.messages || {
  empty: 'No hay registros todavia.',
  error: 'Ocurrio un error al procesar la solicitud.',
  successCreate: 'Registro creado.',
  successUpdate: 'Registro actualizado.',
  successDelete: 'Registro eliminado.'
})

const listStickyHeader = computed(() => entidadSeleccionada.value?.listStickyHeader === true)
const listShowTotals = computed(() => entidadSeleccionada.value?.listShowTotals !== false)
const formLayout = computed(() => entidadSeleccionada.value?.formLayout || 'single')
const confirmSave = computed(() => entidadSeleccionada.value?.confirmSave !== false)
const confirmDelete = computed(() => entidadSeleccionada.value?.confirmDelete !== false)
const enableDuplicate = computed(() => entidadSeleccionada.value?.enableDuplicate !== false)

const apiRoute = computed(() => (entidadSeleccionada.value ? entidadRoute(entidadSeleccionada.value) : ''))
const currentEntityKey = computed(() => String(apiRoute.value || '').toLowerCase())
const isMovementView = computed(() => currentEntityKey.value === 'movement')
const isMovementLineView = computed(() => currentEntityKey.value === 'movement-line' || currentEntityKey.value === 'movementline')
const isOperationAuditView = computed(() => currentEntityKey.value === 'operation-audit' || currentEntityKey.value === 'operationaudit')
const isResourceInstanceView = computed(() => currentEntityKey.value === 'resource-instance' || currentEntityKey.value === 'resourceinstance')
const isStockBalanceView = computed(() => currentEntityKey.value === 'stock-balance' || currentEntityKey.value === 'stockbalance')
const hasOpsCore = computed(() => {
  const keys = new Set(
    entities.value.map(entity => normalizeRouteKey(entityRouteKey(entity)))
  )
  return keys.has('movement')
    && keys.has('movementline')
    && keys.has('resourceinstance')
    && keys.has('stockbalance')
})
const showOpsFlowPanel = computed(() => {
  if (!hasOpsCore.value) return false
  if (!hasPermission('ops.movement.create') || !hasPermission('ops.movementline.create')) return false
  return isMovementView.value || isMovementLineView.value || isStockBalanceView.value || isResourceInstanceView.value
})
const opsQuickTypes = computed(() => OPS_QUICK_TYPES)
const opsMovementTypeItems = computed(() => toSelectOptions(OPS_MOVEMENT_TYPES))
const opsDisableSource = computed(() => isSourceDisabledForType(opsForm.value.movementType))
const opsDisableTarget = computed(() => isTargetDisabledForType(opsForm.value.movementType))
const opsRoutes = computed(() => ({
  movement: resolveEntityPath('movement'),
  movementLine: resolveEntityPath('movementline'),
  stockBalance: resolveEntityPath('stockbalance'),
  resourceInstance: resolveEntityPath('resourceinstance')
}))

function permissionCode(entityKey, action) {
  const normalizedEntity = normalizeRouteKey(entityKey || '')
  const normalizedAction = normalizeRouteKey(action || '')
  if (!normalizedEntity || !normalizedAction) return ''
  return `ops.${normalizedEntity}.${normalizedAction}`
}

function hasPermission(code) {
  if (!code) return true
  if (permissionsBypass.value) return true
  if (!permissionsLoaded.value) return true
  if (!permissionSet.value || permissionSet.value.size === 0) return false
  if (permissionSet.value.has('*')) return true
  return permissionSet.value.has(code.toLowerCase())
}

function hasEntityPermission(action) {
  return hasPermission(permissionCode(currentEntityCanonicalKey.value, action))
}

const canCreateRecords = computed(() => {
  if (!entidadSeleccionada.value) return false
  if (entidadSeleccionada.value.allowCreate === false) return false
  if (isOperationAuditView.value) return false
  return hasEntityPermission('create')
})

const isIncidentesView = computed(() => {
  if (!entidadSeleccionada.value) return false
  const slug = entidadRoute(entidadSeleccionada.value)
  const name = (entidadSeleccionada.value?.name || '').toLowerCase()
  return slug === 'incidentes' || name === 'incidentes'
})

const showAudioRecorder = computed(() => isIncidentesView.value)
const showIncidentMap = computed(() => isIncidentesView.value)

const showRetryJob = computed(() => {
  if (!entidadSeleccionada.value) return false
  const slug = entidadRoute(entidadSeleccionada.value)
  const name = (entidadSeleccionada.value?.name || '').toLowerCase()
  return slug === 'incidente-jobs' || slug === 'incidentejobs' || name === 'incidentejobs' || name === 'incidente-jobs'
})

const showAudioPlayAction = computed(() => {
  if (!entidadSeleccionada.value) return false
  const slug = entidadRoute(entidadSeleccionada.value)
  const name = (entidadSeleccionada.value?.name || '').toLowerCase()
  return slug === 'incidente-audio' || slug === 'incidenteaudio' || name === 'incidenteaudio' || name === 'incidente-audio'
})

const showMapAction = computed(() => isIncidentesView.value)
const showTraceAction = computed(() => isResourceInstanceView.value && hasPermission('ops.operationaudit.timeline'))

const summaryItems = computed(() => {
  const items = []
  const list = registros.value || []
  if (!entidadSeleccionada.value) return items

  items.push({
    label: 'Total',
    value: list.length,
    icon: 'mdi-format-list-bulleted'
  })

  if (isIncidentesView.value) {
    const withCoords = list.filter(item => hasCoords(item)).length
    items.push({
      label: 'Con coordenadas',
      value: withCoords,
      icon: 'mdi-map-marker',
      color: withCoords ? 'green' : 'grey'
    })
    items.push({
      label: 'Sin coordenadas',
      value: Math.max(list.length - withCoords, 0),
      icon: 'mdi-map-marker-off-outline',
      color: 'orange'
    })
  }

  if (showRetryJob.value) {
    const statusCounts = list.reduce((acc, item) => {
      const status = getStatusValue(item)
      if (!status) return acc
      acc[status] = (acc[status] || 0) + 1
      return acc
    }, {})
    if (statusCounts.done) {
      items.push({ label: 'Completados', value: statusCounts.done, icon: 'mdi-check-circle-outline', color: 'green' })
    }
    if (statusCounts.processing || statusCounts.pending || statusCounts.running || statusCounts.queued) {
      const active = (statusCounts.processing || 0) + (statusCounts.pending || 0) + (statusCounts.running || 0) + (statusCounts.queued || 0)
      items.push({ label: 'En proceso', value: active, icon: 'mdi-timer-sand', color: 'orange' })
    }
    if (statusCounts.error) {
      items.push({ label: 'Errores', value: statusCounts.error, icon: 'mdi-alert-circle-outline', color: 'red' })
    }
  }

  return items
})

const summaryMeta = computed(() => {
  const list = registros.value || []
  if (!list.length) return ''
  const timestamps = list
    .map(item => item?.UpdateAt || item?.updateAt || item?.CreatedAt || item?.createdAt)
    .filter(Boolean)
    .map(value => new Date(value).getTime())
    .filter(value => Number.isFinite(value))
  if (!timestamps.length) return ''
  const latest = new Date(Math.max(...timestamps))
  return latest.toLocaleString(locale.value)
})

const mapUrls = computed(() => {
  if (!mapRecord.value) return null
  const coords = getCoords(mapRecord.value)
  if (!coords) return null
  const { lat, lng } = coords
  const delta = 0.01
  const bbox = [
    (lng - delta).toFixed(6),
    (lat - delta).toFixed(6),
    (lng + delta).toFixed(6),
    (lat + delta).toFixed(6)
  ].join(',')
  const marker = `${lat.toFixed(6)},${lng.toFixed(6)}`
  return {
    embed: `https://www.openstreetmap.org/export/embed.html?bbox=${bbox}&layer=mapnik&marker=${marker}`,
    link: `https://www.openstreetmap.org/?mlat=${lat.toFixed(6)}&mlon=${lng.toFixed(6)}#map=18/${lat.toFixed(6)}/${lng.toFixed(6)}`
  }
})

const audioSupported = computed(() => {
  return typeof window !== 'undefined' &&
    navigator?.mediaDevices?.getUserMedia &&
    typeof window.MediaRecorder !== 'undefined'
})

const itemsPerPageOptions = computed(() => config.value?.system?.itemsPerPageOptions || [10, 20, 50])

const showSearch = computed(() => config.value?.system?.showSearch !== false)
const showFilters = computed(() => config.value?.system?.showFilters !== false)

const filterFields = computed(() => listFields.value.filter(f => f.showInFilter !== false).map(f => ({
  title: f.label || f.name || f.columnName,
  value: f.columnName
})))

const filteredRegistros = computed(() => {
  let items = [...registros.value]

  if (search.value) {
    const term = search.value.toLowerCase()
    items = items.filter(item => {
      return listFields.value.some(field => {
        const value = readFieldValue(item, field.columnName)
        return value != null && value.toString().toLowerCase().includes(term)
      })
    })
  }

  if (filterField.value && filterValue.value) {
    const term = filterValue.value.toLowerCase()
    items = items.filter(item => {
      const value = readFieldValue(item, filterField.value)
      return value != null && value.toString().toLowerCase().includes(term)
    })
  }

  return items
})

const sortedRegistros = computed(() => {
  const items = [...filteredRegistros.value]
  const entity = entidadSeleccionada.value
  if (!entity) return items

  const sortFieldId = entity.defaultSortFieldId
  const sortField = campos.value.find(f => f.fieldId === sortFieldId) || pkField.value
  const sortKey = sortField?.columnName
  const dir = entity.defaultSortDirection === 'desc' ? -1 : 1

  if (!sortKey) return items

  items.sort((a, b) => {
    const va = readFieldValue(a, sortKey)
    const vb = readFieldValue(b, sortKey)
    if (va == null && vb == null) return 0
    if (va == null) return -1 * dir
    if (vb == null) return 1 * dir
    if (typeof va === 'number' && typeof vb === 'number') return (va - vb) * dir
    const sa = va.toString().toLowerCase()
    const sb = vb.toString().toLowerCase()
    if (sa < sb) return -1 * dir
    if (sa > sb) return 1 * dir
    return 0
  })

  return items
})

const pageCount = computed(() => {
  const total = sortedRegistros.value.length
  return total === 0 ? 1 : Math.ceil(total / itemsPerPage.value)
})

const paginatedRegistros = computed(() => {
  const start = (page.value - 1) * itemsPerPage.value
  const end = start + itemsPerPage.value
  return sortedRegistros.value.slice(start, end)
})

const headers = computed(() => {
  const cols = listFields.value.map(field => ({
    title: field.label || field.name || field.columnName,
    key: field.columnName
  }))

  return [
    ...cols,
    { title: 'Acciones', key: 'actions', sortable: false }
  ]
})

function buildOpsForm(movementType = 'ingreso') {
  return {
    movementType,
    sourceLocationId: null,
    targetLocationId: null,
    resourceInstanceId: null,
    quantity: 1,
    unitCost: null,
    referenceNo: '',
    notes: '',
    autoConfirm: true
  }
}

function coalesceNumber(value) {
  if (value === null || value === undefined || value === '') return null
  const num = Number(value)
  return Number.isFinite(num) ? num : null
}

function isSourceDisabledForType(movementType) {
  const type = normalizeTextKey(movementType)
  return type === 'ingreso'
}

function isTargetDisabledForType(movementType) {
  const type = normalizeTextKey(movementType)
  return type === 'egreso' || type === 'reserva' || type === 'liberacion'
}

function applyTypeShape(form) {
  const type = normalizeTextKey(form.movementType)
  if (isSourceDisabledForType(type)) form.sourceLocationId = null
  if (isTargetDisabledForType(type)) form.targetLocationId = null
}

function resolveEntityPath(key) {
  const target = normalizeRouteKey(key)
  const entity = runtimeEntities.value.find(ent => normalizeRouteKey(entityRouteKey(ent)) === target)
  return entity ? `/${entidadRoute(entity)}` : ''
}

function irRuta(path) {
  if (!path) return
  router.push(path)
}

async function loadOpsCatalog() {
  const [locationsRes, resourcesRes] = await Promise.all([
    runtimeApi.list('location'),
    runtimeApi.list('resource-instance')
  ])

  const locations = Array.isArray(locationsRes?.data) ? locationsRes.data : []
  const resources = Array.isArray(resourcesRes?.data) ? resourcesRes.data : []

  opsLocationItems.value = locations
    .filter(item => readFieldValue(item, 'IsActive') !== false)
    .map(item => {
      const id = readFieldValue(item, 'Id')
      const code = readFieldValue(item, 'Codigo')
      const name = readFieldValue(item, 'Nombre')
      return {
        value: id,
        title: code && name ? `${code} · ${name}` : (code || name || `#${id}`)
      }
    })
    .filter(item => item.value != null)

  opsResourceItems.value = resources
    .filter(item => readFieldValue(item, 'IsActive') !== false)
    .map(item => {
      const id = readFieldValue(item, 'Id')
      const code = readFieldValue(item, 'CodigoInterno')
      const state = readFieldValue(item, 'Estado')
      return {
        value: id,
        title: code && state ? `${code} · ${state}` : (code || `#${id}`)
      }
    })
    .filter(item => item.value != null)
}

function validateOpsForm(form) {
  const type = normalizeTextKey(form.movementType)
  if (!OPS_MOVEMENT_TYPES.includes(type)) return 'Tipo de movimiento inválido.'
  if (form.resourceInstanceId == null) return 'Selecciona un recurso.'
  const qty = coalesceNumber(form.quantity)
  if (qty == null || qty <= 0) return 'La cantidad debe ser mayor a 0.'

  const source = coalesceNumber(form.sourceLocationId)
  const target = coalesceNumber(form.targetLocationId)

  if (type === 'ingreso' && target == null) return 'Ingreso requiere ubicación destino.'
  if ((type === 'egreso' || type === 'reserva' || type === 'liberacion') && source == null) {
    return `${prettifyLabel(type)} requiere ubicación origen.`
  }
  if (type === 'transferencia') {
    if (source == null || target == null) return 'Transferencia requiere origen y destino.'
    if (String(source) === String(target)) return 'Origen y destino deben ser distintos.'
  }
  if (type === 'ajuste' && source == null && target == null) return 'Ajuste requiere origen o destino.'
  if (type === 'ajuste' && source != null && target != null) return 'Ajuste debe indicar solo una ubicación.'
  return ''
}

async function resolveMovementIdByReference(referenceNo) {
  const { data } = await runtimeApi.list('movement')
  const items = Array.isArray(data) ? data : []
  const matches = items
    .filter(item => String(readFieldValue(item, 'ReferenceNo') || '') === referenceNo)
    .map(item => coalesceNumber(readFieldValue(item, 'Id')))
    .filter(id => id != null)
    .sort((a, b) => b - a)
  return matches[0] ?? null
}

async function abrirOperacionGuiada(type = 'ingreso') {
  if (!hasPermission('ops.movement.create') || !hasPermission('ops.movementline.create')) {
    opsError.value = 'No tienes permisos para crear operaciones guiadas.'
    return
  }

  opsError.value = ''
  opsDialog.value = true
  opsForm.value = buildOpsForm(type)
  applyTypeShape(opsForm.value)
  try {
    await loadOpsCatalog()
  } catch (err) {
    opsError.value = extractApiErrorMessage(err, 'No se pudo cargar catálogo de ubicaciones/recursos.')
  }
}

async function guardarOperacionGuiada() {
  if (!hasPermission('ops.movement.create') || !hasPermission('ops.movementline.create')) {
    opsError.value = 'No tienes permisos para crear operaciones guiadas.'
    return
  }

  opsError.value = ''
  applyTypeShape(opsForm.value)
  const validationError = validateOpsForm(opsForm.value)
  if (validationError) {
    opsError.value = validationError
    return
  }

  const source = coalesceNumber(opsForm.value.sourceLocationId)
  const target = coalesceNumber(opsForm.value.targetLocationId)
  const quantity = Number(opsForm.value.quantity)
  const unitCost = coalesceNumber(opsForm.value.unitCost)
  const now = new Date().toISOString()
  const referenceNo = (opsForm.value.referenceNo || '').trim() || `OPS-${Date.now()}`

  const movementPayload = {
    movementtype: normalizeTextKey(opsForm.value.movementType),
    status: 'borrador',
    sourcelocationid: source,
    targetlocationid: target,
    referenceno: referenceNo,
    notes: (opsForm.value.notes || '').trim() || null,
    operationat: now,
    createdby: 'runtime-ui',
    createdat: now
  }

  opsSubmitting.value = true
  try {
    await runtimeApi.create('movement', movementPayload)
    const movementId = await resolveMovementIdByReference(referenceNo)
    if (movementId == null) {
      throw new Error('Se creó el movimiento pero no se pudo resolver el Id.')
    }

    const movementLinePayload = {
      movementid: movementId,
      resourceinstanceid: coalesceNumber(opsForm.value.resourceInstanceId),
      quantity,
      unitcost: unitCost,
      serie: null,
      lote: null,
      createdat: now
    }
    await runtimeApi.create('movement-line', movementLinePayload)

    if (opsForm.value.autoConfirm) {
      if (!hasPermission('ops.movement.confirm')) {
        throw new Error('No tienes permiso para confirmar movimientos.')
      }
      const movementRes = await runtimeApi.get('movement', movementId)
      const movement = movementRes?.data || {}
      const confirmPayload = buildMovementUpdatePayload(movement, 'confirmado')
      await runtimeApi.update('movement', movementId, confirmPayload)
    }

    opsDialog.value = false
    showToast('Operación creada correctamente.', 'green')
    if (apiRoute.value === 'movement' || apiRoute.value === 'movement-line' || apiRoute.value === 'stock-balance') {
      await cargarDatos()
    }
  } catch (err) {
    opsError.value = extractApiErrorMessage(err, 'No se pudo ejecutar la operación guiada.')
  } finally {
    opsSubmitting.value = false
  }
}

function normalizeTextKey(value) {
  return String(value || '').trim().toLowerCase()
}

function readFieldValue(item, columnName) {
  if (!item || typeof item !== 'object' || !columnName) return undefined
  if (item[columnName] !== undefined) return item[columnName]
  const target = normalizeTextKey(columnName)
  const exactKey = Object.keys(item).find(k => normalizeTextKey(k) === target)
  if (exactKey) return item[exactKey]
  const compactTarget = target.replace(/[^a-z0-9]/g, '')
  const looseKey = Object.keys(item).find(k => normalizeTextKey(k).replace(/[^a-z0-9]/g, '') === compactTarget)
  return looseKey ? item[looseKey] : undefined
}

function prettifyLabel(value) {
  return String(value || '')
    .replace(/[_-]+/g, ' ')
    .replace(/([a-z0-9])([A-ZÁÉÍÓÚÑ])/g, '$1 $2')
    .replace(/\s+/g, ' ')
    .trim()
}

function toSelectOptions(values) {
  return values.map(v => ({ title: prettifyLabel(v), value: v }))
}

function fieldOptionsFor(entityKey, fieldKey) {
  if (entityKey === 'movement' && fieldKey === 'movementtype') return toSelectOptions(OPS_MOVEMENT_TYPES)
  if (entityKey === 'movement' && fieldKey === 'status') return toSelectOptions(OPS_MOVEMENT_STATUSES)
  if (entityKey === 'resourcedefinition' && fieldKey === 'trackmode') return toSelectOptions(OPS_TRACK_MODES)
  if (entityKey === 'resourceinstance' && fieldKey === 'estado') return toSelectOptions(OPS_RESOURCE_STATES)
  if (entityKey === 'location' && fieldKey === 'tipo') return toSelectOptions(OPS_LOCATION_TYPES)
  if (entityKey === 'attributedefinition' && fieldKey === 'datatype') return toSelectOptions(OPS_ATTRIBUTE_TYPES)
  if (entityKey === 'operationaudit' && fieldKey === 'result') return toSelectOptions(OPS_AUDIT_RESULTS)
  return null
}

function shouldHideFieldInForm(entityKey, fieldKey) {
  if (fieldKey === 'id') return true
  if (fieldKey === 'correlationid') return true
  if (fieldKey === 'payloadjson') return true
  if (fieldKey === 'stockdisponible') return true
  if (fieldKey.endsWith('createdat') || fieldKey.endsWith('updatedat') || fieldKey.endsWith('executedat')) return true
  if (entityKey === 'operationaudit') return true
  return false
}

function defaultFieldValue(entityKey, fieldKey, dataType) {
  if (entityKey === 'movement' && fieldKey === 'status') return 'borrador'
  if (entityKey === 'movement' && fieldKey === 'movementtype') return 'ingreso'
  if (entityKey === 'resourcedefinition' && fieldKey === 'trackmode') return 'none'
  if (entityKey === 'resourceinstance' && fieldKey === 'estado') return 'activo'
  if (fieldKey === 'isactive') return true
  if (String(dataType || '').toLowerCase().includes('bool')) return false
  return undefined
}

function statusChipColor(value) {
  const key = normalizeTextKey(value)
  if (!key) return 'grey'
  if (key === 'confirmado' || key === 'ok' || key === 'activo' || key === 'done') return 'green'
  if (key === 'borrador' || key === 'pending' || key === 'processing' || key === 'running') return 'orange'
  if (key === 'anulado' || key === 'error' || key === 'inactivo' || key === 'bloqueado') return 'red'
  if (key === 'warning') return 'amber'
  return 'blue-grey'
}

function fieldLabelOverride(entityKey, fieldKey, fallbackLabel) {
  const byEntity = {
    movement: {
      movementtype: 'Tipo',
      sourcelocationid: 'Origen',
      targetlocationid: 'Destino',
      referenceno: 'Ref.',
      operationat: 'Fecha'
    },
    movementline: {
      movementid: 'Movimiento',
      resourceinstanceid: 'Recurso',
      quantity: 'Cant.',
      unitcost: 'Costo U.',
      createdat: 'Fecha'
    },
    stockbalance: {
      resourceinstanceid: 'Recurso',
      locationid: 'Ubicacion',
      stockreal: 'Real',
      stockreservado: 'Reservado',
      stockdisponible: 'Disponible'
    },
    attributedefinition: {
      resourcedefinitionid: 'Recurso'
    },
    attributevalue: {
      resourceinstanceid: 'Recurso',
      attributedefinitionid: 'Atributo'
    },
    resourceinstance: {
      resourcedefinitionid: 'Tipo recurso',
      codigointerno: 'Codigo'
    },
    location: {
      parentlocationid: 'Padre'
    }
  }
  const entityLabels = byEntity[entityKey] || {}
  if (entityLabels[fieldKey]) return entityLabels[fieldKey]
  if (fieldKey.endsWith('id') && fieldKey !== 'id') {
    return prettifyLabel(fieldKey.slice(0, -2))
  }
  return fallbackLabel
}

function normalizeRouteKey(value) {
  return normalizeTextKey(value).replace(/[^a-z0-9]/g, '')
}

function resolveRelationKey(fieldKey) {
  const key = normalizeTextKey(fieldKey)
  if (!key) return null
  if (key === 'movementid') return 'movement'
  if (key === 'resourceinstanceid') return 'resourceinstance'
  if (key === 'locationid' || key === 'sourcelocationid' || key === 'targetlocationid' || key === 'parentlocationid') return 'location'
  if (key === 'resourcedefinitionid') return 'resourcedefinition'
  if (key === 'attributedefinitionid') return 'attributedefinition'
  return null
}

function relationKeyFromRoute(routeKey) {
  const normalizedRoute = normalizeRouteKey(routeKey)
  for (const [relationKey, route] of Object.entries(RELATION_ROUTES)) {
    if (normalizeRouteKey(route) === normalizedRoute) return relationKey
  }
  return null
}

function relationLabelFor(relationKey, item, id) {
  const fallback = `#${id}`
  if (!item || typeof item !== 'object') return fallback
  if (relationKey === 'movement') {
    const ref = readFieldValue(item, 'ReferenceNo')
    const type = readFieldValue(item, 'MovementType')
    const status = readFieldValue(item, 'Status')
    const base = ref || type || fallback
    return status ? `${base} · ${status}` : base
  }
  if (relationKey === 'resourceinstance') {
    const code = readFieldValue(item, 'CodigoInterno')
    const serie = readFieldValue(item, 'Serie')
    return code || serie || fallback
  }
  if (relationKey === 'location') {
    const code = readFieldValue(item, 'Codigo')
    const name = readFieldValue(item, 'Nombre')
    return code || name || fallback
  }
  if (relationKey === 'resourcedefinition' || relationKey === 'attributedefinition') {
    const code = readFieldValue(item, 'Codigo')
    const name = readFieldValue(item, 'Nombre')
    return code || name || fallback
  }
  return fallback
}

function getRelationDisplay(relationKey, id) {
  if (!relationKey || id == null) return null
  const lookup = relationLookups.value?.[relationKey] || {}
  return lookup[id] || lookup[String(id)] || null
}

function normalizeConfig() {
  if (!config.value?.system) config.value.system = {}
  const sys = config.value.system
  sys.primaryColor = sys.primaryColor || '#2563eb'
  sys.secondaryColor = sys.secondaryColor || '#0ea5e9'
  sys.density = sys.density || 'comfortable'
  sys.fontFamily = sys.fontFamily || "Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif"
  sys.uiMode = sys.uiMode || 'enterprise'
  sys.locale = sys.locale || 'es-AR'
  sys.currency = sys.currency || 'ARS'

  if (!Array.isArray(config.value.entities)) config.value.entities = []

  config.value.entities.forEach(entity => {
    const entityKey = entityCanonicalKey(entity)

    if (entity.showInMenu === undefined) entity.showInMenu = true
    if (!entity.menuIcon || entity.menuIcon === 'mdi-table') {
      entity.menuIcon = OPS_ENTITY_ICONS[entityKey] || entity.menuIcon || 'mdi-table'
    }
    if (
      OPS_ENTITY_LABELS[entityKey] &&
      (
        !entity.menuLabel ||
        normalizeTextKey(entity.menuLabel) === normalizeTextKey(entity.name || '') ||
        normalizeTextKey(entity.menuLabel) === normalizeTextKey(entity.displayName || '')
      )
    ) {
      entity.menuLabel = OPS_ENTITY_LABELS[entityKey]
    }
    if (entity.routeSlug === undefined) entity.routeSlug = ''
    if (entity.listStickyHeader === undefined) entity.listStickyHeader = false
    if (entity.listShowTotals === undefined) entity.listShowTotals = true
    if (!entity.defaultSortDirection) entity.defaultSortDirection = 'asc'
    if (!entity.formLayout) entity.formLayout = 'single'
    if (OPS_SECTIONED_ENTITY_KEYS.has(entityKey) && entity.formLayout === 'single') {
      entity.formLayout = 'sections'
    }
    if (entity.confirmSave === undefined) entity.confirmSave = true
    if (entity.confirmDelete === undefined) entity.confirmDelete = true
    if (entity.enableDuplicate === undefined) entity.enableDuplicate = true
    if (entity.allowCreate === undefined) entity.allowCreate = true
    if (entity.allowEdit === undefined) entity.allowEdit = true
    if (entity.allowDelete === undefined) entity.allowDelete = true

    if (entityKey === 'movement') {
      entity.menuIcon = entity.menuIcon || 'mdi-swap-horizontal'
      entity.messages = {
        ...(entity.messages || {}),
        confirmDelete: 'Eliminar movimiento? Si tiene lineas en borrador tambien se eliminaran.',
        confirmSave: 'Guardar movimiento?',
        error: 'No se pudo procesar el movimiento.'
      }
    }

    if (entityKey === 'movementline') {
      entity.menuIcon = entity.menuIcon || 'mdi-format-list-bulleted-square'
      entity.messages = {
        ...(entity.messages || {}),
        confirmDelete: 'Eliminar linea de movimiento?',
        confirmSave: 'Guardar linea de movimiento?',
        error: 'No se pudo procesar la linea de movimiento.'
      }
    }

    if (entityKey === 'operationaudit') {
      entity.allowCreate = false
      entity.allowEdit = false
      entity.allowDelete = false
      entity.enableDuplicate = false
      entity.confirmDelete = false
      entity.confirmSave = false
      entity.messages = {
        ...(entity.messages || {}),
        error: 'No se pudo cargar la auditoria.'
      }
    }

    entity.messages = {
      empty: 'No hay registros todavia.',
      error: 'Ocurrio un error al procesar la solicitud.',
      successCreate: 'Registro creado.',
      successUpdate: 'Registro actualizado.',
      successDelete: 'Registro eliminado.',
      ...(entity.messages || {})
    }
    if (!Array.isArray(entity.fields)) entity.fields = []
    entity.fields.forEach(field => {
      const fieldKey = normalizeTextKey(field.columnName || field.name)
      const fieldDataType = String(field.dataType || '').toLowerCase()
      const fieldLayout = OPS_FIELD_LAYOUT?.[entityKey]?.[fieldKey]
      if (field.placeholder === undefined) field.placeholder = ''
      if (field.helpText === undefined) field.helpText = ''
      if (field.inputType === undefined) field.inputType = ''
      if (field.section === undefined) field.section = 'General'
      if (fieldLayout && (!field.section || field.section === 'General')) field.section = fieldLayout.section
      if (field.sectionOrder === undefined || field.sectionOrder === null) {
        field.sectionOrder = fieldLayout?.sectionOrder ?? 999
      }
      if (field.formOrder === undefined || field.formOrder === null) {
        field.formOrder = fieldLayout?.formOrder ?? 999
      }
      if (field.format === undefined) field.format = ''
      if (field.min === undefined) field.min = null
      if (field.max === undefined) field.max = null
      if (field.pattern === undefined) field.pattern = ''
      if (field.quickToggle === undefined) field.quickToggle = false
      if (field.showInList === undefined) field.showInList = true
      const baseLabel = prettifyLabel(field.columnName || field.name)
      if (!field.label || field.label === field.name || field.label === field.columnName || field.label === baseLabel) {
        field.label = fieldLabelOverride(entityKey, fieldKey, baseLabel)
      }
      if (field.showInForm === undefined) field.showInForm = true
      if (shouldHideFieldInForm(entityKey, fieldKey)) {
        field.showInForm = false
      }
      if (!field.options) {
        field.options = fieldOptionsFor(entityKey, fieldKey)
      }
      if (!field.inputType && Array.isArray(field.options) && field.options.length > 0) {
        field.inputType = 'select'
      }
      if (!field.inputType && fieldDataType.includes('datetime')) {
        field.inputType = 'datetime'
      }
      if (!field.inputType && (fieldDataType.includes('date') || fieldKey.includes('vencimiento'))) {
        field.inputType = 'date'
      }
      if (!field.inputType && (fieldDataType.includes('decimal') || fieldDataType.includes('int') || fieldDataType.includes('numeric') || fieldDataType.includes('float'))) {
        field.inputType = 'number'
      }
      if (!field.inputType && (fieldDataType.includes('bool') || fieldDataType.includes('bit'))) {
        field.inputType = 'checkbox'
      }
      if (!field.format && (fieldDataType.includes('datetime') || fieldDataType.includes('date') || fieldKey.endsWith('at'))) {
        field.format = fieldDataType.includes('date') && !fieldDataType.includes('time') ? 'date' : 'datetime'
      }
      if (!field.format && (fieldKey === 'status' || fieldKey === 'estado' || fieldKey === 'result')) {
        field.format = 'status-chip'
      }
      if (fieldKey === 'payloadjson' || fieldKey === 'correlationid') {
        field.showInList = false
      }
      if ((fieldKey.endsWith('createdat') || fieldKey.endsWith('updatedat')) && entityKey !== 'operationaudit') {
        field.showInList = false
      }
      if (field.defaultValue === undefined) {
        const inferredDefault = defaultFieldValue(entityKey, fieldKey, fieldDataType)
        if (inferredDefault !== undefined) field.defaultValue = inferredDefault
      }
    })
  })
}

function resolverEntidad() {
  if (!runtimeEntities.value.length) {
    entidadSeleccionada.value = null
    return
  }

  const target = entitySlug.value
    ? runtimeEntities.value.find(ent => entidadRoute(ent) === entitySlug.value)
    : runtimeEntities.value[0]

  if (!target) {
    router.replace(`/${entidadRoute(runtimeEntities.value[0])}`)
    return
  }

  entidadSeleccionada.value = target
  cargarDatos()
}

function irEntidad(entidad) {
  const slug = entidadRoute(entidad)
  router.push(`/${slug}`)
}

function isCurrentEntity(entidad) {
  if (!entidad) return false
  return entityCanonicalKey(entidad) === currentEntityCanonicalKey.value
}

function irGrupo(groupId) {
  const entitiesInGroup = groupedRuntimeEntities.value[groupId] || []
  if (!entitiesInGroup.length) return
  irEntidad(entitiesInGroup[0])
}

function buildRelationMap(relationKey, items) {
  const map = {}
  ;(items || []).forEach(item => {
    const id = readFieldValue(item, 'Id')
    if (id == null) return
    map[id] = relationLabelFor(relationKey, item, id)
  })
  return map
}

async function loadRelationLookup(relationKey, force = false) {
  const route = RELATION_ROUTES[relationKey]
  if (!route) return
  const current = relationLookups.value?.[relationKey] || {}
  if (!force && Object.keys(current).length > 0) return
  try {
    const { data } = await runtimeApi.list(route)
    const items = Array.isArray(data) ? data : (data?.items || [])
    relationLookups.value = {
      ...relationLookups.value,
      [relationKey]: buildRelationMap(relationKey, items)
    }
  } catch {
    // Lookup opcional; no bloquea el runtime.
  }
}

async function ensureRelationLookups(force = false) {
  const keys = new Set()
  for (const field of listFields.value) {
    const relationKey = resolveRelationKey(field?.columnName || field?.name)
    if (relationKey) keys.add(relationKey)
  }
  if (!keys.size) return
  await Promise.all(Array.from(keys).map(key => loadRelationLookup(key, force)))
}

async function cargarDatos(options = {}) {
  if (!entidadSeleccionada.value) return
  const silent = options.silent === true
  if (!silent) {
    loading.value = true
    error.value = ''
  }
  try {
    const { data } = await runtimeApi.list(apiRoute.value)
    const items = Array.isArray(data) ? data : (data?.items || [])
    registros.value = items.map(item => normalizeRecord(item))
    const currentRelationKey = relationKeyFromRoute(apiRoute.value)
    if (currentRelationKey) {
      relationLookups.value = {
        ...relationLookups.value,
        [currentRelationKey]: buildRelationMap(currentRelationKey, registros.value)
      }
    }
    await ensureRelationLookups()
    if (isMovementView.value) {
      movementStatusById.value = Object.fromEntries(
        registros.value
          .map(row => [getRecordId(row), getStatusValue(row)])
          .filter(([id]) => id != null)
      )
    } else if (isMovementLineView.value) {
      await ensureMovementStatusCache()
    }
    if (isIncidentesView.value) {
      const currentId = mapRecord.value ? getRecordId(mapRecord.value) : null
      if (currentId != null) {
        const stillThere = registros.value.find(r => String(getRecordId(r)) === String(currentId))
        if (stillThere) {
          mapRecord.value = stillThere
          return
        }
      }
      const withCoords = registros.value.find(r => hasCoords(r))
      mapRecord.value = withCoords || registros.value[0] || null
    }
  } catch (err) {
    if (!silent) {
      error.value = extractApiErrorMessage(err, entityMessages.value.error)
    }
  } finally {
    if (!silent) {
      loading.value = false
    }
  }
}

function normalizeRecord(record) {
  if (!record || typeof record !== 'object') return record
  const copy = { ...record }
  campos.value.forEach(field => {
    const key = field.columnName
    if (!key) return
    if (copy[key] === undefined) {
      const value = readFieldValue(copy, key)
      if (value !== undefined) copy[key] = value
    }
  })
  return copy
}

function extractApiErrorMessage(err, fallback = 'Ocurrio un error al procesar la solicitud.') {
  const payload = err?.response?.data
  if (typeof payload === 'string' && payload.trim()) return payload
  if (payload?.message) return payload.message
  if (payload?.error) return payload.error
  if (err?.message) return err.message
  return fallback
}

function buildDefaultRecord() {
  const record = {}
  for (const field of campos.value) {
    const key = field?.columnName
    if (!key || field.showInForm === false || field.isPrimaryKey || field.isIdentity) continue
    if (field.defaultValue !== undefined) {
      record[key] = field.defaultValue
    }
  }
  return record
}

async function ensureMovementStatusCache(force = false) {
  if (!isMovementLineView.value && !force) return
  if (!force && Object.keys(movementStatusById.value).length) return
  try {
    const { data } = await runtimeApi.list('movement')
    const items = Array.isArray(data) ? data : (data?.items || [])
    const map = {}
    items.forEach(item => {
      const id = readFieldValue(item, 'Id')
      if (id == null) return
      map[id] = getStatusValue(item)
    })
    movementStatusById.value = map
  } catch {
    // Mantener silencioso para no bloquear listados.
  }
}

function getMovementStatusFromLine(item) {
  if (!item || typeof item !== 'object') return ''
  const movementId = readFieldValue(item, 'MovementId')
  if (movementId == null) return ''
  return normalizeTextKey(movementStatusById.value[movementId])
}

function isMovementCancelled(item) {
  return getStatusValue(item) === 'anulado'
}

function isMovementConfirmed(item) {
  return getStatusValue(item) === 'confirmado'
}

function isMovementDraft(item) {
  return getStatusValue(item) === 'borrador'
}

function isMovementLineLocked(item) {
  const status = getMovementStatusFromLine(item)
  return status === 'confirmado' || status === 'anulado'
}

function canEditItem(item) {
  if (!entidadSeleccionada.value || !item) return false
  if (entidadSeleccionada.value.allowEdit === false) return false
  if (!hasEntityPermission('update')) return false
  if (isMovementView.value && (isMovementConfirmed(item) || isMovementCancelled(item))) return false
  if (isMovementLineView.value && isMovementLineLocked(item)) return false
  return true
}

function canDeleteItem(item) {
  if (!entidadSeleccionada.value || !item) return false
  if (entidadSeleccionada.value.allowDelete === false) return false
  if (!hasEntityPermission('delete')) return false
  if (isMovementView.value && isMovementConfirmed(item)) return false
  if (isMovementLineView.value && isMovementLineLocked(item)) return false
  return true
}

function canDuplicateItem(item) {
  if (!enableDuplicate.value || !canCreateRecords.value || !item) return false
  if (isMovementView.value && isMovementConfirmed(item)) return false
  if (isMovementLineView.value && isMovementLineLocked(item)) return false
  return true
}

function canConfirmMovement(item) {
  if (!isMovementView.value || !item) return false
  if (!hasPermission('ops.movement.confirm')) return false
  return isMovementDraft(item)
}

function isConfirmingMovement(item) {
  const id = getRecordId(item)
  return id != null && Boolean(confirmingIds.value[id])
}

function buildMovementUpdatePayload(item, nextStatus) {
  const payload = { ...item }
  payload.MovementType = readFieldValue(item, 'MovementType') ?? payload.MovementType
  payload.Status = nextStatus
  payload.SourceLocationId = readFieldValue(item, 'SourceLocationId') ?? null
  payload.TargetLocationId = readFieldValue(item, 'TargetLocationId') ?? null
  payload.ReferenceNo = readFieldValue(item, 'ReferenceNo') ?? null
  payload.Notes = readFieldValue(item, 'Notes') ?? null
  payload.OperationAt = readFieldValue(item, 'OperationAt') ?? null
  payload.CreatedBy = readFieldValue(item, 'CreatedBy') ?? null
  payload.CreatedAt = readFieldValue(item, 'CreatedAt') ?? null
  return payload
}

async function confirmarMovimiento(item) {
  if (!canConfirmMovement(item)) return
  if (!pkField.value) return

  const id = readFieldValue(item, pkField.value.columnName)
  if (id == null) return

  const ok = window.confirm('Confirmar movimiento? Esta accion aplica stock y no permite editar lineas luego.')
  if (!ok) return

  confirmingIds.value = { ...confirmingIds.value, [id]: true }
  try {
    const payload = buildMovementUpdatePayload(item, 'confirmado')
    await runtimeApi.update(apiRoute.value, id, payload)
    showToast('Movimiento confirmado.', 'green')
    await cargarDatos()
    await ensureMovementStatusCache(true)
  } catch (err) {
    window.alert(extractApiErrorMessage(err, entityMessages.value.error))
  } finally {
    const next = { ...confirmingIds.value }
    delete next[id]
    confirmingIds.value = next
  }
}

function nuevoRegistro() {
  if (!canCreateRecords.value) return
  dialogMode.value = 'create'
  registroActual.value = buildDefaultRecord()
  dialog.value = true
}

function editarRegistro(item) {
  if (!canEditItem(item)) return
  dialogMode.value = 'edit'
  registroActual.value = { ...item }
  dialog.value = true
}

function duplicarRegistro(item) {
  if (!canDuplicateItem(item)) return
  dialogMode.value = 'duplicate'
  registroActual.value = { ...item }
  dialog.value = true
}

async function eliminarRegistro(item) {
  if (!pkField.value) return
  if (!canDeleteItem(item)) return
  if (confirmDelete.value) {
    const ok = window.confirm(entityMessages.value.confirmDelete || 'Eliminar registro?')
    if (!ok) return
  }

  try {
    const id = readFieldValue(item, pkField.value.columnName)
    if (id == null) return
    await runtimeApi.remove(apiRoute.value, id)
    await cargarDatos()
  } catch (err) {
    window.alert(extractApiErrorMessage(err, entityMessages.value.error))
  }
}

async function copiarRegistro(item) {
  const record = item || {}
  const lines = campos.value.map(field => {
    const label = field.label || field.name || field.columnName || 'Campo'
    const value = formatValueForCopy(record, field)
    return `${label}: ${value}`
  })
  const text = lines.join('\n')

  try {
    if (navigator?.clipboard?.writeText) {
      await navigator.clipboard.writeText(text)
      showToast('Datos copiados.', 'green')
      return
    }
    fallbackCopy(text)
  } catch {
    fallbackCopy(text)
  }
}

function normalizeTraceItem(item) {
  const executedRaw = readFieldValue(item, 'ExecutedAt')
  const executedDate = executedRaw ? new Date(executedRaw) : null
  return {
    id: readFieldValue(item, 'Id'),
    operationName: readFieldValue(item, 'OperationName') || '-',
    entityName: readFieldValue(item, 'EntityName') || '-',
    entityId: readFieldValue(item, 'EntityId'),
    result: normalizeTextKey(readFieldValue(item, 'Result')),
    message: readFieldValue(item, 'Message') || '',
    actor: readFieldValue(item, 'Actor') || '',
    executedAt: executedDate && !Number.isNaN(executedDate.getTime())
      ? executedDate.toLocaleString(locale.value)
      : String(executedRaw || '')
  }
}

function inferTraceResourceLabel(item) {
  const code = readFieldValue(item, 'CodigoInterno')
  const serie = readFieldValue(item, 'Serie')
  const state = readFieldValue(item, 'Estado')
  const base = code || serie || `#${getRecordId(item)}`
  return state ? `${base} · ${state}` : base
}

async function abrirTrazabilidad(item) {
  const id = getRecordId(item)
  if (id == null) return
  traceDialog.value = true
  traceLoading.value = true
  traceError.value = ''
  traceItems.value = []
  traceResourceLabel.value = inferTraceResourceLabel(item)
  try {
    const { data } = await runtimeApi.getResourceTimeline(id)
    const rows = Array.isArray(data) ? data : (data?.items || [])
    traceItems.value = rows.map(normalizeTraceItem)
  } catch (err) {
    traceError.value = extractApiErrorMessage(err, 'No se pudo cargar la trazabilidad.')
  } finally {
    traceLoading.value = false
  }
}

function parseCoord(value) {
  if (value === null || value === undefined) return null
  if (typeof value === 'number') return Number.isFinite(value) ? value : null
  const normalized = value.toString().replace(',', '.')
  const num = Number(normalized)
  return Number.isFinite(num) ? num : null
}

function getCoords(item) {
  if (!item) return null
  const lat = parseCoord(item.Lat ?? item.lat)
  const lng = parseCoord(item.Lng ?? item.lng)
  if (lat == null || lng == null) return null
  return { lat, lng }
}

function getAudioFilePath(item) {
  if (!item) return ''
  return item.Filepath || item.filepath || item.FilePath || item.filePath || ''
}

function hasAudioFile(item) {
  return Boolean(getAudioFilePath(item))
}

function hasCoords(item) {
  return Boolean(getCoords(item))
}

function abrirMapa(item) {
  if (!item) return
  mapRecord.value = item
  const urls = mapUrls.value
  if (!urls?.link) return
  window.open(urls.link, '_blank', 'noopener')
}

async function abrirAudioPlayback(item) {
  const id = getRecordId(item)
  if (id == null) return
  clearAudioPlayback()
  audioPlayDialog.value = true
  audioPlayLoading.value = true
  audioPlayError.value = ''
  audioPlayItem.value = item
  try {
    const token = localStorage.getItem('token') || ''
    if (!token) {
      audioPlayError.value = 'Token no disponible para reproducir.'
      return
    }
    audioPlayUrl.value = runtimeApi.getIncidenteAudioStreamUrl(id, token)
    audioPlayMime.value = 'audio/mpeg'
  } catch (err) {
    audioPlayError.value = 'No se pudo cargar el audio.'
  } finally {
    audioPlayLoading.value = false
  }
}

function cerrarAudioPlayback() {
  audioPlayDialog.value = false
  clearAudioPlayback()
}

function clearAudioPlayback() {
  if (audioPlayUrl.value) {
    URL.revokeObjectURL(audioPlayUrl.value)
  }
  audioPlayUrl.value = ''
  audioPlayMime.value = ''
  audioPlayError.value = ''
  audioPlayLoading.value = false
  audioPlayItem.value = null
}

function onAudioPlayError(event) {
  const media = event?.target
  const code = media?.error?.code
  const message = code === 1
    ? 'Reproduccion abortada.'
    : code === 2
      ? 'Error de red al cargar el audio.'
      : code === 3
        ? 'Error al decodificar el audio.'
        : code === 4
          ? 'Formato de audio no soportado.'
          : 'No se pudo reproducir el audio.'
  audioPlayError.value = message
}

function getRecordId(item) {
  if (!item || typeof item !== 'object') return null
  const pk = pkField.value?.columnName
  if (pk) {
    const byPk = readFieldValue(item, pk)
    if (byPk !== undefined) return byPk
  }
  const byId = readFieldValue(item, 'Id')
  if (byId !== undefined) return byId
  return null
}

function updateRegistroLocal(id, patch) {
  if (id == null) return
  registros.value = registros.value.map(item => {
    const currentId = getRecordId(item)
    if (currentId == null || String(currentId) !== String(id)) return item
    return { ...item, ...patch }
  })
}

function isRetrying(item) {
  const id = getRecordId(item)
  return id != null && Boolean(retryingIds.value[id])
}

async function reintentarJob(item) {
  const id = getRecordId(item)
  if (id == null) return
  retryingIds.value = { ...retryingIds.value, [id]: true }
  try {
    updateRegistroLocal(id, {
      Status: 'processing',
      Step: 'processing',
      UpdateAt: new Date().toISOString()
    })
    await runtimeApi.retryIncidenteJob(id)
    showToast('Job reintentado.', 'orange')
    await cargarDatos({ silent: true })
    startAutoRefresh()
  } catch (err) {
    showToast('No se pudo reintentar el job.', 'red')
  } finally {
    const next = { ...retryingIds.value }
    delete next[id]
    retryingIds.value = next
  }
}

function shouldShowProgress(item, col) {
  const key = String(col?.key || '').toLowerCase()
  if (key !== 'status' && key !== 'step') return false
  const raw = readFieldValue(item, col?.key)
  const value = raw == null ? '' : raw.toString().toLowerCase()
  if (value === 'processing' || value === 'pending' || value === 'running' || value === 'queued') return true
  if (key === 'status' && isRetrying(item)) return true
  return false
}

function getStatusValue(item) {
  if (!item || typeof item !== 'object') return ''
  const raw = readFieldValue(item, 'Status') ?? readFieldValue(item, 'Step') ?? readFieldValue(item, 'Estado') ?? ''
  return raw == null ? '' : raw.toString().toLowerCase()
}

function formatValueForCopy(item, field) {
  if (!field?.columnName) return ''
  const res = formattedCell(item, { key: field.columnName })
  return res?.text ?? ''
}

function fallbackCopy(text) {
  const el = document.createElement('textarea')
  el.value = text
  el.setAttribute('readonly', '')
  el.style.position = 'absolute'
  el.style.left = '-9999px'
  document.body.appendChild(el)
  el.select()
  try {
    document.execCommand('copy')
    showToast('Datos copiados.', 'green')
  } catch {
    window.alert('No se pudo copiar.')
  } finally {
    document.body.removeChild(el)
  }
}

function showToast(message, color = 'green') {
  toastMessage.value = message
  toastColor.value = color
  toastOpen.value = false
  requestAnimationFrame(() => {
    toastOpen.value = true
  })
}

async function cargarPermisos() {
  permissionsLoaded.value = false
  permissionsBypass.value = false
  permissionSet.value = new Set()

  try {
    const { data } = await runtimeApi.getMyPermissions()
    const rawPermissions = Array.isArray(data?.Permissions)
      ? data.Permissions
      : (Array.isArray(data?.permissions) ? data.permissions : [])

    const normalized = new Set(
      rawPermissions
        .map(value => String(value || '').trim().toLowerCase())
        .filter(Boolean)
    )

    const isAdmin = Boolean(data?.IsAdmin ?? data?.isAdmin)
    const enabled = data?.Enabled ?? data?.enabled
    if (isAdmin) normalized.add('*')

    permissionSet.value = normalized
    permissionsBypass.value = enabled === false
  } catch (err) {
    const status = err?.response?.status
    if (status === 404) {
      permissionSet.value = new Set(['*'])
      permissionsBypass.value = true
    } else {
      permissionSet.value = new Set()
      permissionsBypass.value = false
    }
  } finally {
    permissionsLoaded.value = true
  }
}

async function toggleQuickField(item) {
  if (!quickToggleField.value) return
  if (!pkField.value) return

  const payload = { ...item }
  const key = quickToggleField.value.columnName
  payload[key] = !payload[key]

  try {
    const id = readFieldValue(item, pkField.value.columnName)
    if (id == null) return
    await runtimeApi.update(apiRoute.value, id, payload)
    await cargarDatos()
  } catch (err) {
    window.alert(extractApiErrorMessage(err, entityMessages.value.error))
  }
}

function formattedCell(item, col) {
  const field = campos.value.find(f => f.columnName === col.key)
  if (!field) return { text: readFieldValue(item, col.key), isChip: false }

  let value = readFieldValue(item, col.key)
  const format = field.format
  const dataType = String(field.dataType || '').toLowerCase()
  const relationKey = resolveRelationKey(field.columnName || field.name)

  if (value == null) return { text: '', isChip: false }

  if (relationKey) {
    const relation = getRelationDisplay(relationKey, value)
    if (relation) return { text: relation, isChip: false }
    return { text: `#${value}`, isChip: false }
  }

  if (format === 'uppercase') {
    value = String(value).toUpperCase()
  }

  if (format === 'money') {
    const formatter = new Intl.NumberFormat(locale.value, {
      style: 'currency',
      currency: currency.value
    })
    return { text: formatter.format(value), isChip: false }
  }

  if (format === 'status-chip') {
    return { text: value, isChip: true, color: statusChipColor(value) }
  }

  if (format === 'datetime' || dataType.includes('datetime')) {
    const date = new Date(value)
    if (!Number.isNaN(date.getTime())) {
      return { text: date.toLocaleString(locale.value), isChip: false }
    }
  }

  if (format === 'date' || (dataType.includes('date') && !dataType.includes('time') && !dataType.includes('datetime'))) {
    const date = new Date(value)
    if (!Number.isNaN(date.getTime())) {
      return { text: date.toLocaleDateString(locale.value), isChip: false }
    }
  }

  if (format === 'badge') {
    return { text: value, isChip: true, color: value ? 'green' : 'red' }
  }

  if (dataType.includes('bit') || dataType.includes('bool')) {
    return { text: value ? 'Si' : 'No', isChip: true, color: value ? 'green' : 'grey' }
  }

  return { text: value, isChip: false }
}

function abrirAudioDialog() {
  audioDialog.value = true
  audioError.value = ''
  audioSuccess.value = false
  audioJobId.value = null
  audioProcessing.value = false
  audioJobStatus.value = ''
  audioJobLastError.value = ''
}

function cerrarAudioDialog() {
  stopRecording(true)
  clearRecording()
  audioDialog.value = false
  stopAudioPolling()
}

function preferredMimeType() {
  const types = [
    'audio/webm;codecs=opus',
    'audio/webm',
    'audio/ogg;codecs=opus',
    'audio/ogg',
    'audio/mp4'
  ]
  if (typeof window === 'undefined' || !window.MediaRecorder) return ''
  for (const type of types) {
    if (MediaRecorder.isTypeSupported(type)) return type
  }
  return ''
}

async function startRecording() {
  audioError.value = ''
  audioSuccess.value = false
  audioJobId.value = null
  if (!audioSupported.value) {
    audioError.value = 'Grabacion no soportada por el navegador.'
    return
  }
  try {
    mediaStream = await navigator.mediaDevices.getUserMedia({ audio: true })
    const mimeType = preferredMimeType()
    mediaRecorder = mimeType ? new MediaRecorder(mediaStream, { mimeType }) : new MediaRecorder(mediaStream)
    audioChunks = []
    mediaRecorder.ondataavailable = event => {
      if (event.data && event.data.size > 0) audioChunks.push(event.data)
    }
    mediaRecorder.onstop = () => {
      const blob = new Blob(audioChunks, { type: mediaRecorder?.mimeType || 'audio/webm' })
      audioBlob.value = blob
      audioMime.value = blob.type
      audioUrl.value = URL.createObjectURL(blob)
      audioChunks = []
    }
    mediaRecorder.start()
    audioRecording.value = true
  } catch (err) {
    audioError.value = 'No se pudo acceder al microfono.'
  }
}

function stopRecording(silent = false) {
  try {
    if (mediaRecorder && mediaRecorder.state === 'recording') {
      mediaRecorder.stop()
    }
  } catch {
    if (!silent) audioError.value = 'Error al detener la grabacion.'
  } finally {
    audioRecording.value = false
    if (mediaStream) {
      mediaStream.getTracks().forEach(track => track.stop())
      mediaStream = null
    }
  }
}

function clearRecording() {
  if (audioUrl.value) {
    URL.revokeObjectURL(audioUrl.value)
  }
  audioUrl.value = ''
  audioBlob.value = null
  audioMime.value = ''
}

function extensionForMime(mime) {
  const type = (mime || '').toLowerCase()
  if (type.includes('webm')) return 'webm'
  if (type.includes('ogg')) return 'ogg'
  if (type.includes('mp4') || type.includes('m4a')) return 'm4a'
  if (type.includes('wav')) return 'wav'
  return 'webm'
}

async function uploadRecording() {
  if (!audioBlob.value) return
  audioUploading.value = true
  audioError.value = ''
  audioSuccess.value = false
  try {
    const ext = extensionForMime(audioMime.value)
    const file = new File([audioBlob.value], `audio_${Date.now()}.${ext}`, {
      type: audioMime.value || 'audio/webm'
    })
    const formData = new FormData()
    formData.append('audio', file)
    if (audioDescripcion.value) {
      formData.append('descripcion', audioDescripcion.value)
    }
    const { data } = await runtimeApi.uploadAudio(formData)
    audioSuccess.value = true
    audioJobId.value = data?.jobId || null
    audioProcessing.value = true
    audioJobStatus.value = 'pending'
    audioJobLastError.value = ''
    await cargarDatos()
    startAudioPolling()
  } catch (err) {
    audioError.value = 'No se pudo enviar el audio.'
  } finally {
    audioUploading.value = false
  }
}

function normalizeJob(job) {
  if (!job || typeof job !== 'object') return null
  const raw = job.raw || job
  const keys = Object.keys(raw)
  const lower = new Map(keys.map(k => [k.toLowerCase(), k]))
  const pick = (...names) => {
    for (const name of names) {
      if (raw[name] !== undefined) return raw[name]
      const match = lower.get(String(name).toLowerCase())
      if (match) return raw[match]
    }
    return undefined
  }
  return {
    id: pick('id'),
    status: pick('status'),
    step: pick('step'),
    lastError: pick('lastError', 'lasterror'),
    attempts: pick('attempts')
  }
}

async function refreshAudioJobStatus() {
  if (!audioJobId.value) return
  try {
    const { data } = await runtimeApi.list('incidente-jobs')
    const items = Array.isArray(data) ? data : (data?.items || [])
    const job = items.map(normalizeJob).find(j => j && String(j.id) === String(audioJobId.value))
    if (!job) return
    audioJobStatus.value = (job.status || '').toString().toLowerCase()
    audioJobLastError.value = job.lastError || ''
    if (audioJobStatus.value === 'done' || audioJobStatus.value === 'error') {
      audioProcessing.value = false
      stopAudioPolling()
    }
  } catch {
    // si falla, dejamos de pollear para no saturar
    stopAudioPolling()
  }
}

function startAutoRefresh() {
  stopAutoRefresh()
  if (!autoRefreshEnabled.value) return
  autoRefreshTimer = setInterval(async () => {
    if (autoRefreshInFlight) return
    autoRefreshInFlight = true
    try {
      await cargarDatos({ silent: true })
    } finally {
      autoRefreshInFlight = false
    }
  }, autoRefreshIntervalMs.value)
}

function stopAutoRefresh() {
  if (autoRefreshTimer) {
    clearInterval(autoRefreshTimer)
    autoRefreshTimer = null
  }
}

function startAudioPolling() {
  stopAudioPolling()
  audioPollTimer = setInterval(async () => {
    if (audioPollInFlight) return
    audioPollInFlight = true
    try {
      await refreshAudioJobStatus()
      await cargarDatos({ silent: true })
    } finally {
      audioPollInFlight = false
    }
  }, 3000)
}

function stopAudioPolling() {
  if (audioPollTimer) {
    clearInterval(audioPollTimer)
    audioPollTimer = null
  }
}

watch(
  () => entitySlug.value,
  () => resolverEntidad()
)

watch(
  () => opsForm.value.movementType,
  () => applyTypeShape(opsForm.value)
)

watch(autoRefreshEnabled, enabled => {
  if (enabled) startAutoRefresh()
  else stopAutoRefresh()
})

watch(itemsPerPage, () => {
  page.value = 1
})

watch(audioPlayDialog, open => {
  if (!open) clearAudioPlayback()
})

onMounted(async () => {
  normalizeConfig()
  if (config.value?.system?.defaultItemsPerPage) {
    itemsPerPage.value = config.value.system.defaultItemsPerPage
  }
  await cargarPermisos()
  resolverEntidad()
  if (autoRefreshEnabled.value) {
    startAutoRefresh()
  }
})

onBeforeUnmount(() => {
  stopRecording(true)
  stopAudioPolling()
  stopAutoRefresh()
  clearAudioPlayback()
})
</script>

<style scoped>
.runtime-container {
  font-family: var(--sb-font, "Manrope", system-ui, sans-serif);
}

.sb-page-header {
  padding: 12px;
  background: var(--sb-surface);
  border-radius: calc(var(--sb-radius) + 2px);
  box-shadow: var(--sb-shadow);
  border: 1px solid var(--sb-border-soft);
  position: relative;
}

.sb-page-icon {
  width: 48px;
  height: 48px;
  background: var(--sb-primary-soft);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 12px;
}

.sb-page-header::after {
  content: '';
  position: absolute;
  top: 14px;
  left: 14px;
  width: 6px;
  height: calc(100% - 28px);
  border-radius: 999px;
  background: linear-gradient(180deg, var(--sb-primary), var(--sb-secondary));
  opacity: 0.7;
}

.card {
  border-radius: 16px;
}

.side-card {
  box-shadow: 0 6px 16px rgba(15, 23, 42, 0.08);
}

.summary-card {
  background: color-mix(in srgb, var(--sb-surface) 96%, transparent);
}

.summary-grid {
  display: grid;
  gap: 12px;
}

.summary-item {
  display: flex;
  gap: 10px;
  align-items: center;
}

.summary-icon {
  width: 34px;
  height: 34px;
  border-radius: 10px;
  background: var(--sb-primary-soft);
  display: flex;
  align-items: center;
  justify-content: center;
}

.summary-label {
  font-size: 0.75rem;
  color: var(--sb-muted);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.summary-value {
  font-size: 1.05rem;
  font-weight: 600;
}

.summary-meta {
  font-size: 0.8rem;
  color: var(--sb-muted);
  display: flex;
  gap: 6px;
}

.summary-meta-label {
  font-weight: 600;
}

.module-nav-card {
  border: 1px solid var(--sb-border-soft);
  background: color-mix(in srgb, var(--sb-surface) 95%, transparent);
}

.module-nav-head {
  display: flex;
  align-items: center;
  gap: 8px;
}

.module-nav-entities {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 10px;
}

.module-flow {
  margin-top: 12px;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.module-flow-buttons {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.ops-flow-card {
  border: 1px solid var(--sb-border-soft);
  background: color-mix(in srgb, var(--sb-surface) 92%, transparent);
}

.ops-flow-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.table :deep(th) {
  font-weight: 600;
  text-transform: none;
  letter-spacing: 0.02em;
}

.table :deep(th),
.table :deep(td) {
  padding: 4px 8px;
  font-size: 0.85rem;
  line-height: 1.2;
  vertical-align: middle;
}

.table :deep(tbody td) {
  color: var(--sb-text);
}

.table :deep(th .v-data-table-header__content) {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 140px;
}

.row-selected {
  background: var(--sb-primary-soft);
}

.map-embed iframe {
  border-radius: 10px;
  box-shadow: 0 8px 20px rgba(15, 23, 42, 0.12);
}

.map-card :deep(.v-card-title) {
  font-weight: 600;
}

.cell-text {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  text-overflow: ellipsis;
  word-break: break-word;
  max-width: 260px;
}

.audio-player {
  width: 100%;
}

.actions-td {
  width: 140px;
  min-width: 140px;
}

.actions-cell {
  display: grid;
  grid-template-columns: repeat(3, 28px);
  gap: 6px;
  justify-content: center;
  align-content: center;
}

.actions-cell :deep(.v-btn) {
  min-width: 28px;
  height: 28px;
  border-radius: 10px;
  background: color-mix(in srgb, var(--sb-border) 55%, transparent);
}

.actions-cell :deep(.v-icon) {
  font-size: 16px;
}

.actions-cell :deep(.v-btn:hover) {
  background: var(--sb-primary-soft);
}

.cta-button {
  border-radius: 999px;
  font-weight: 600;
  letter-spacing: 0.3px;
  text-transform: none;
}

.cta-button.primary {
  background: linear-gradient(135deg, var(--sb-primary), var(--sb-secondary));
  color: #fff;
  box-shadow: 0 8px 20px rgba(37, 99, 235, 0.25);
}

.cta-button.ghost {
  color: var(--sb-text);
  border: 1px solid color-mix(in srgb, var(--sb-border) 70%, transparent);
  background: color-mix(in srgb, var(--sb-surface) 88%, transparent);
}

.trace-list {
  display: grid;
  gap: 10px;
  max-height: 50vh;
  overflow: auto;
}

.trace-item {
  border: 1px solid var(--sb-border-soft);
  border-radius: 12px;
  padding: 10px 12px;
  background: color-mix(in srgb, var(--sb-surface) 92%, transparent);
}

.trace-item-head {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.trace-item-meta {
  margin-top: 4px;
  font-size: 0.82rem;
  color: var(--sb-text-soft);
}

.trace-item-message {
  margin-top: 6px;
  font-size: 0.9rem;
}
</style>
