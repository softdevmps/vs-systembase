import api from './axios';

export default {
  getBySystem(systemId) {
    return api.get(`/sistemas/${systemId}/entidades`);
  },

  getById(systemId, id) {
    return api.get(`/sistemas/${systemId}/entidades/${id}`);
  },

  getByName(systemId, name) {
    return api.get(`/sistemas/${systemId}/entidades/by-name/${name}`);
  },

  crear(systemId, data) {
    return api.post(`/sistemas/${systemId}/entidades`, data);
  },

  editar(systemId, id, data) {
    return api.put(`/sistemas/${systemId}/entidades/${id}`, data);
  }
};
