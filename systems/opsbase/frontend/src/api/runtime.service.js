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

  getResourceTimeline(resourceInstanceId) {
    return api.get(`/operation-audit/timeline/resource-instance/${resourceInstanceId}`)
  },

  getMyPermissions() {
    return api.get('/security/me/permissions')
  },

  createOpsRecepcion(payload) {
    return api.post('/ops-flow/recepcion', payload)
  },

  createOpsDespacho(payload) {
    return api.post('/ops-flow/despacho', payload)
  },

  getOpsDepositosMapa() {
    return api.get('/ops-dashboard/depositos/mapa')
  },

  getOpsDepositoContext(locationId, limit = 40) {
    return api.get(`/ops-dashboard/depositos/${locationId}/contexto`, {
      params: { limit }
    })
  },

  createOpsDeposito(payload) {
    return api.post('/ops-dashboard/depositos', payload)
  },

  updateOpsDeposito(locationId, payload) {
    return api.put(`/ops-dashboard/depositos/${locationId}`, payload)
  },

  deleteOpsDeposito(locationId) {
    return api.delete(`/ops-dashboard/depositos/${locationId}`)
  }
}
