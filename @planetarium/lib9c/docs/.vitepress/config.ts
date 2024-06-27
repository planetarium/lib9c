import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config
export default defineConfig({
  title: "@planetarium/lib9c",
  description: "Documentation for @planetarium/lib9c package",
  themeConfig: {
    // https://vitepress.dev/reference/default-theme-config
    nav: [
      { text: 'Docs', link: '/docs/index.md' },
      { text: 'API Reference', link: 'https://jsr.io/@planetarium/lib9c' },
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/planetarium/lib9c/tree/development/%40planetarium/lib9c' }
    ],

    i18nRouting: true,
    sidebar: [
      {
        link: '/docs/index.md',
        text: "Introduction"
      },
      {
        link: '/docs/installation.md',
        text: "Installation"
      },
      {
        link: '/docs/actions.md',
        text: "Actions"
      },
      {
        link: '/docs/utility.md',
        text: "Utility"
      }
    ],
    outline: 'deep'
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
          { text: '문서', link: '/ko/docs/index.md' },
          { text: 'API Reference', link: 'https://jsr.io/@planetarium/lib9c' },
        ],
        sidebar: [
          {
            link: '/ko/docs/index.md',
            text: "소개"
          },
          {
            link: '/ko/docs/installation.md',
            text: "설치"
          },
          {
            link: '/ko/docs/actions.md',
            text: "액션"
          },
          {
            link: '/ko/docs/utility.md',
            text: "유틸리티"
          }
        ]
      }
    },
  }
})
