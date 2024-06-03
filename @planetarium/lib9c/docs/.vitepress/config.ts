import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config
export default defineConfig({
  title: "@planetarium/lib9c",
  description: "Documentation for @planetarium/lib9c package",
  themeConfig: {
    // https://vitepress.dev/reference/default-theme-config
    nav: [
      { text: 'Docs', link: '/docs' },
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/planetarium/lib9c/tree/development/%40planetarium/lib9c' }
    ],

    i18nRouting: true,
    sidebar: [
      {
        link: '/docs',
        items: [
          {
            link: '/docs',
            text: "Introduction"
          },
          {
            link: '/docs/actions',
            text: "Actions"
          },
          {
            link: '/docs/utility',
            text: "Utility"
          }
        ]
      }
    ],
  },
  locales: {
    root: {
      label: 'English',
      lang: 'en',
    },
    ko: {
      label: 'Korean',
      lang: 'ko',
      themeConfig: {
        i18nRouting: true,
        nav: [
          { text: '문서', link: '/ko/docs' },
        ],
        sidebar: [
          {
            link: '/ko/docs',
            items: [
              {
                link: '/ko/docs',
                text: "소개"
              },
              {
                link: '/ko/docs/actions',
                text: "액션"
              },
              {
                link: '/ko/docs/utility',
                text: "유틸리티"
              }
            ]
          }
        ]
      }
    },
  }
})
