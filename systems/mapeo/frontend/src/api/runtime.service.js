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

  uploadAudio(formData) {
    return api.post('/incidentes/audio', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })
  },

  retryIncidenteJob(id) {
    return api.post(`/incidente-jobs/${id}/retry`)
  },

  downloadIncidenteAudio(id) {
    return api.get(`/incidente-audio/${id}/file`, {
      responseType: 'arraybuffer'
    })
  },

  getIncidenteAudioStreamUrl(id, token) {
    const base = api.defaults.baseURL || ''
    const safeToken = token ? encodeURIComponent(token) : ''
    const qs = safeToken ? `?token=${safeToken}` : ''
    return `${base}/incidente-audio/${id}/stream${qs}`
  }
}
