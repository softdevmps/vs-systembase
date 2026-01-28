<template>
    <v-card>
        <v-data-table :headers="headers" :items="roles">

            <!-- ACTIVO -->
            <template #item.activo="{ item }">
                <v-switch v-model="item.activo" inset color="green" @change="$emit('cambiar-estado', item)" />
            </template>

            <!-- ACCIONES -->
            <template #item.actions="{ item }">
                <v-btn icon color="primary" @click="$emit('editar', item)">
                    <v-icon>mdi-pencil</v-icon>
                </v-btn>

                <v-btn icon color="secondary" @click="$emit('menus', item)">
                    <v-icon>mdi-menu</v-icon>
                </v-btn>
            </template>

        </v-data-table>
    </v-card>
</template>

<script>
export default {
    props: {
        roles: {
            type: Array,
            required: true
        },
        headers: {
            type: Array,
            required: true
        }
    }
};
</script>
