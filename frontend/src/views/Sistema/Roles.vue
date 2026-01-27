<template>
    <v-container fluid>

        <!-- HEADER -->
        <v-row align="center" class="mb-4">
            <v-col cols="6" class="d-flex align-center">
                <v-icon class="mr-2">mdi-shield-account</v-icon>
                <h2 class="mb-0">Roles</h2>
            </v-col>

            <v-col cols="6" class="text-right">
                <v-btn color="primary" @click="abrirDialogCrear">
                    + NUEVO ROL
                </v-btn>
            </v-col>
        </v-row>

        <!-- TABLA -->
        <v-card>
            <v-card-text class="pa-0">
                <v-data-table :headers="headers" :items="roles" item-key="id" dense class="elevation-1">
                    <!-- ACTIVO -->
                    <template v-slot:item.activo="{ item }">
                        <div class="d-flex justify-center">
                            <v-switch v-model="item.activo" inset hide-details @change="cambiarEstado(item)" />
                        </div>
                    </template>

                    <!-- ACCIONES -->
                    <template v-slot:item.acciones="{ item }">
                        <v-btn icon @click="editarRol(item)">
                            <v-icon>mdi-pencil</v-icon>
                        </v-btn>

                        <v-btn icon @click="abrirAsignarMenus(item)">
                            <v-icon>mdi-view-list</v-icon>
                        </v-btn>
                    </template>
                </v-data-table>
            </v-card-text>
        </v-card>

        <!-- DIALOG CREAR / EDITAR -->
        <rol-dialog v-model="dialogRol" :rol="rolSeleccionado" @guardado="cargarRoles" />

        <!-- DIALOG ASIGNAR MENÚS -->
        <rol-menus-dialog v-model="dialogMenus" :rol="rolSeleccionado" />

    </v-container>
</template>

<script>
import rolService from '../../api/rol.service';
import RolDialog from '../../components/roles/RolDialog.vue';
import RolMenusDialog from '../../components/roles/RolMenusDialog.vue';

export default {
    components: {
        RolDialog,
        RolMenusDialog
    },

    data() {
        return {
            roles: [],
            dialogRol: false,
            dialogMenus: false,
            rolSeleccionado: null,

            headers: [
                { text: 'ID', value: 'id', width: 80 },
                { text: 'Rol', value: 'nombre' },
                { text: 'Activo', value: 'activo', align: 'center', width: 120 },
                { text: 'Acciones', value: 'acciones', sortable: false, align: 'center', width: 140 }
            ]
        };
    },

    mounted() {
        this.cargarRoles();
    },

    methods: {
        async cargarRoles() {
            const { data } = await rolService.getAll();
            this.roles = data;
        },

        abrirDialogCrear() {
            this.rolSeleccionado = null;
            this.dialogRol = true;
        },

        editarRol(rol) {
            this.rolSeleccionado = { ...rol };
            this.dialogRol = true;
        },

        abrirAsignarMenus(rol) {
            this.rolSeleccionado = rol;
            this.dialogMenus = true; // 👈 SOLO ESTO
        },

        async cambiarEstado(rol) {
            await rolService.cambiarEstado(rol.id, rol.activo);
        }
    }
};
</script>
