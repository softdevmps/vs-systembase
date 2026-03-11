import api from './axios'

export default {
  list(route) {
    return api.get(`/${route}`)
  },

  get(route, id) {
    return api.get(`/${route}/${id}`)
  },

  create(route, payload) {
    return api.post(`/${route}`, payload)
  },

  update(route, id, payload) {
    return api.put(`/${route}/${id}`, payload)
  },

  remove(route, id) {
    return api.delete(`/${route}/${id}`)
  },

  getOverview(take = 12) {
    return api.get('/aibase/overview', { params: { take } })
  },

  listProjectRuns(projectId, take = 25) {
    return api.get(`/aibase/projects/${projectId}/runs`, { params: { take } })
  },

  getProjectWorkflow(projectId) {
    return api.get(`/aibase/projects/${projectId}/workflow`)
  },

  bootstrap(payload = {}) {
    return api.post('/aibase/bootstrap', payload)
  },

  triggerProjectRun(projectId, runType, inputJson = null) {
    return api.post(`/aibase/projects/${projectId}/run`, { runType, inputJson })
  },

  uploadProjectDatasetFile(projectId, file, sourceType = null) {
    const form = new FormData()
    form.append('file', file)
    if (sourceType) form.append('sourceType', sourceType)
    return api.post(`/aibase/projects/${projectId}/dataset/upload`, form, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
  },

  listProjectDatasetSources(projectId) {
    return api.get(`/aibase/projects/${projectId}/dataset/sources`)
  },

  generateProjectDataset(projectId, payload = {}) {
    return api.post(`/aibase/projects/${projectId}/dataset/generate`, payload)
  },

  mergeProjectDatasets(projectId, payload = {}) {
    return api.post(`/aibase/projects/${projectId}/dataset/merge`, payload)
  },

  runAll(projectId, payload = {}) {
    return api.post(`/aibase/projects/${projectId}/run-all`, payload)
  },

  deployAssets(projectId) {
    return api.get(`/aibase/projects/${projectId}/deploy-assets`)
  },

  exportDeployBundle(projectId, payload = {}) {
    return api.post(`/aibase/projects/${projectId}/deploy-export`, payload)
  },

  inferProject(projectId, input, contextJson = null) {
    return api.post(`/aibase/projects/${projectId}/infer`, { input, contextJson })
  },

  inferMetrics(projectId, { take = 20 } = {}) {
    return api.get(`/aibase/projects/${projectId}/infer-metrics`, { params: { take } })
  },

  assistantSuggest(prompt, stage = null, projectId = null) {
    return api.post('/aibase/assistant/suggest', { prompt, stage, projectId })
  },

  dockerStatus(stackName = null, composeFile = null, envFile = null) {
    return api.get('/aibase/docker/status', { params: { stackName, composeFile, envFile } })
  },

  dockerServices(stackName = null, composeFile = null, envFile = null) {
    return api.get('/aibase/docker/services', { params: { stackName, composeFile, envFile } })
  },

  dockerUp(payload = {}) {
    return api.post('/aibase/docker/up', payload)
  },

  dockerDown(payload = {}) {
    return api.post('/aibase/docker/down', payload)
  },

  dockerRestart(payload = {}) {
    return api.post('/aibase/docker/restart', payload)
  },

  dockerLogs({ stackName = null, composeFile = null, envFile = null, service = null, tail = 200 } = {}) {
    return api.get('/aibase/docker/logs', { params: { stackName, composeFile, envFile, service, tail } })
  },

  dockerServiceAction(service, action, stackName = null, composeFile = null, envFile = null) {
    return api.post(`/aibase/docker/services/${encodeURIComponent(service)}/action`, { action, stackName, composeFile, envFile })
  }
}
