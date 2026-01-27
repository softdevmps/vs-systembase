import api from './axios';

export default {
    getAll() {
        return api.get('/roles');
    },

    getById(id) {
        return api.get(`/roles/${id}`);
    },

    crear(data) {
        return api.post('/roles', data);
    },

    editar(id, data) {
        return api.put(`/roles/${id}`, data);
    },

    cambiarEstado(id, activo) {
        return api.put(`/roles/${id}/estado`, null, {
            params: { activo }
        });
    },

    asignarMenus(id, menusIds) {
        return api.put(`/roles/${id}/menus`, {
            menusIds
        });
    }
};
