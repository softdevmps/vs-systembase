// Styles
import 'vuetify/styles'

// Vuetify core
import { createVuetify } from 'vuetify'

// Components & directives (CLAVE)
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'

// Icons
import { aliases, mdi } from 'vuetify/iconsets/mdi'
import '@mdi/font/css/materialdesignicons.css'

export const vuetify = createVuetify({
    components,
    directives,
    icons: {
        defaultSet: 'mdi',
        aliases,
        sets: {
            mdi
        }
    }
})
